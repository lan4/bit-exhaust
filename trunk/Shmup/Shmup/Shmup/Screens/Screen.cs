#region Using

using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Gui;
#if !SILVERLIGHT

using FlatRedBall.Graphics.Model;
#endif

using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Graphics;



using PolygonSaveList = FlatRedBall.Content.Polygon.PolygonSaveList;
using System.Threading;

#endregion

// Test

namespace Shmup.Screens
{
    public enum AsyncLoadingState
    {
        NotStarted,
        LoadingScreen,
        Done
    }

    public class Screen
    {
        #region Fields

        protected Camera mCamera;
        protected Layer mLayer;

        public bool ShouldRemoveLayer
        {
            get;
            set;
        }


        protected List<Screen> mPopups = new List<Screen>();

        private string mContentManagerName;


        // The following are objects which belong to the screen.
        // These are removed by the Screen when it is Destroyed
        protected SpriteList mSprites = new SpriteList();
        protected List<SpriteGrid> mSpriteGrids = new List<SpriteGrid>();
        protected PositionedObjectList<SpriteFrame> mSpriteFrames = new PositionedObjectList<SpriteFrame>();

        protected List<IDrawableBatch> mDrawableBatches = new List<IDrawableBatch>();
        // End of objects which belong to the Screen.

        // These variables control the flow from one Screen to the next.


        protected Scene mLastLoadedScene;
        private bool mIsActivityFinished;
        private string mNextScreen;

        private bool mManageSpriteGrids;

        internal Screen mNextScreenToLoadAsync;



        #endregion

        #region Properties

        public int ActivityCallCount
        {
            get;
            set;
        }

        public string ContentManagerName
        {
            get { return mContentManagerName; }
        }

        #region XML Docs
        /// <summary>
        /// Gets and sets whether the activity is finished for a particular screen.
        /// </summary>
        /// <remarks>
        /// If activity is finished, then the ScreenManager or parent
        /// screen (if the screen is a popup) knows to destroy the screen
        /// and loads the NextScreen class.</remarks>
        #endregion
        public bool IsActivityFinished
        {
            get { return mIsActivityFinished; }
            set { mIsActivityFinished = value; }

        }


        public AsyncLoadingState AsyncLoadingState
        {
            get;
            private set;
        }


        public Layer Layer
        {
            get { return mLayer; }
			set { mLayer = value;}
        }


        public bool ManageSpriteGrids
        {
            get { return mManageSpriteGrids; }
            set { mManageSpriteGrids = value; }
        }

        #region XML Docs
        /// <summary>
        /// The fully qualified path of the Screen-inheriting class that this screen is 
        /// to link to.
        /// </summary>
        /// <remarks>
        /// This property is read by the ScreenManager when IsActivityFinished is
        /// set to true.  Therefore, this must always be set to some value before
        /// or in the same frame as when IsActivityFinished is set to true.
        /// </remarks>
        #endregion
        public string NextScreen
        {
            get { return mNextScreen; }
            set { mNextScreen = value; }
        }

        protected bool UnloadsContentManagerWhenDestroyed
        {
            get;
            set;
        }

        #endregion

        #region Methods

        #region Constructor

        public Screen(string contentManagerName)
        {
            ShouldRemoveLayer = true;
            UnloadsContentManagerWhenDestroyed = true;
            mContentManagerName = contentManagerName;
            mManageSpriteGrids = true;
            IsActivityFinished = false;

            mLayer = ScreenManager.NextScreenLayer;
        }

        #endregion

        #region Public Methods


        public virtual void Activity(bool firstTimeCalled)
        {
            if (mManageSpriteGrids)
            {
                for (int i = 0; i < mSpriteGrids.Count; i++)
                {
                    SpriteGrid sg = mSpriteGrids[i];
                    sg.Manage();
                }
            }

            for (int i = mPopups.Count - 1; i > -1; i--)
            {
                Screen popup = mPopups[i];

                popup.Activity(false);
                popup.ActivityCallCount++;

                if (popup.IsActivityFinished)
                {
                    string nextPopup = popup.NextScreen;

                    popup.Destroy();
                    mPopups.RemoveAt(i);

                    if (nextPopup != "" && nextPopup != null)
                    {
                        LoadPopup(nextPopup, false);
                    }
                }
            }
        }

        Type asyncScreenTypeToLoad = null;


        public void StartAsyncLoad(string screenType)
        {
            if (AsyncLoadingState == Screens.AsyncLoadingState.LoadingScreen)
            {
                throw new InvalidOperationException("This Screen is already loading a Screen of type " + asyncScreenTypeToLoad);
            }
            else if (AsyncLoadingState == Screens.AsyncLoadingState.Done)
            {
                throw new InvalidOperationException("This Screen has already loaded a Screen of type " + asyncScreenTypeToLoad);
            }
            else
            {

                asyncScreenTypeToLoad = Type.GetType(screenType);

                if (asyncScreenTypeToLoad == null)
                {
                    throw new Exception("Could not find the type " + screenType);
                }
                AsyncLoadingState = AsyncLoadingState.LoadingScreen;

                ThreadStart threadStart = new ThreadStart(PerformAsyncLoad);

                Thread thread = new Thread(threadStart);

                thread.Start();
            }
        }

        private void PerformAsyncLoad()
        {
#if XBOX360
            
            // We can not use threads 0 or 2  
            Thread.CurrentThread.SetProcessorAffinity(4);
            mNextScreenToLoadAsync = (Screen)Activator.CreateInstance(asyncScreenTypeToLoad);
#else
            mNextScreenToLoadAsync = (Screen)Activator.CreateInstance(asyncScreenTypeToLoad, new object[0]);
#endif
            // Don't add it to the manager!
            mNextScreenToLoadAsync.Initialize(false);

            AsyncLoadingState = AsyncLoadingState.Done;
        }

        public virtual void Initialize(bool addToManagers)
        {

        }


        public virtual void AddToManagers()
        {
        }


        public virtual void Destroy()
        {
            if (mLastLoadedScene != null)
            {
                mLastLoadedScene.Clear();
            }

			
            FlatRedBall.Debugging.Debugger.DestroyText();
			
            // All of the popups should be destroyed as well
            foreach (Screen s in mPopups)
                s.Destroy();

            SpriteManager.RemoveSpriteList<Sprite>(mSprites);

            // It's common for users to forget to add Particle Sprites
            // to the mSprites SpriteList.  This will either create leftover
            // particles when the next screen loads or will throw an assert when
            // the ScreenManager checks if there are any leftover Sprites.  To make
            // things easier we'll just clear the Particle Sprites here.  If you don't
            // want this done (not likely), remove the following line, but only do so if
            // you really know what you're doing!
            SpriteManager.RemoveAllParticleSprites();

            // Destory all SpriteGrids that belong to this Screen
            foreach (SpriteGrid sg in mSpriteGrids)
                sg.Destroy();


            // Destroy all SpriteFrames that belong to this Screen
            while (mSpriteFrames.Count != 0)
                SpriteManager.RemoveSpriteFrame(mSpriteFrames[0]);

            if (UnloadsContentManagerWhenDestroyed && mContentManagerName != FlatRedBallServices.GlobalContentManager)
            {
                FlatRedBallServices.Unload(mContentManagerName);
            }

            if (ShouldRemoveLayer && mLayer != null)
            {
                SpriteManager.RemoveLayer(mLayer);
            }

        }

        #region XML Docs
        /// <summary>Tells the screen that we are done and wish to move to the
        /// supplied screen</summary>
        /// <param>Fully Qualified Type of the screen to move to</param>
        #endregion
        public void MoveToScreen(string screenClass)
        {
            IsActivityFinished = true;
            NextScreen = screenClass;
        }

        #endregion

        #region Protected Methods

        public T LoadPopup<T>(Layer layerToLoadPopupOn) where T : Screen
        {
            T loadedScreen = ScreenManager.LoadScreen<T>(layerToLoadPopupOn);
            mPopups.Add(loadedScreen);
            return loadedScreen;
        }

        public Screen LoadPopup(string popupToLoad, Layer layerToLoadPopupOn)
        {
            return LoadPopup(popupToLoad, layerToLoadPopupOn, true);
        }

        public Screen LoadPopup(string popupToLoad, Layer layerToLoadPopupOn, bool addToManagers)
        {
            Screen loadedScreen = ScreenManager.LoadScreen(popupToLoad, layerToLoadPopupOn, addToManagers);
            mPopups.Add(loadedScreen);
            return loadedScreen;
        }

        public Screen LoadPopup(string popupToLoad, bool useNewLayer)
        {
            Screen loadedScreen = ScreenManager.LoadScreen(popupToLoad, useNewLayer);
            mPopups.Add(loadedScreen);
            return loadedScreen;
        }

        #endregion

        #endregion
    }
}
