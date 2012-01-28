using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Math.Geometry;


// Be sure to replace:
// 1.  The namespace
// 2.  The class name
// 3.  The constructor (should be the same as the class name)


namespace Shmup
{
    public class Bullet : PositionedObject
    {
        #region Fields

        // Here you'd define things that your Entity contains, like Sprites
        // or Circles:
        private Sprite mVisibleRepresentation;
        private Sprite mVisibleRepresentation2;
        private Circle mCollision;

        // Keep the ContentManager for easy access:
        string mContentManagerName;

        public bool IsReady;

        public bool IsFriendly;

        #endregion

        #region Properties


        // Here you'd define properties for things
        // you want to give other Entities and game code
        // access to, like your Collision property:
        public Circle Collision
        {
            get { return mCollision; }
        }

        #endregion

        #region Methods

        // Constructor
        public Bullet(string contentManagerName)
        {
            // Set the ContentManagerName and call Initialize:
            mContentManagerName = contentManagerName;

            IsReady = true;
            IsFriendly = false;

            // If you don't want to add to managers, make an overriding constructor
            Initialize(true);
        }

        protected virtual void Initialize(bool addToManagers)
        {
            // Here you can preload any content you will be using
            // like .scnx files or texture files.

            if (addToManagers)
            {
                AddToManagers(null);
            }
        }

        public virtual void AddToManagers(Layer layerToAddTo)
        {
            // Add the Entity to the SpriteManager
            // so it gets managed properly (velocity, acceleration, attachments, etc.)
            SpriteManager.AddPositionedObject(this);

            // Here you may want to add your objects to the engine.  Use layerToAddTo
            // when adding if your Entity supports layers.  Make sure to attach things
            // to this if appropriate.
            mVisibleRepresentation = SpriteManager.AddSprite(@"Content\foeBullet", mContentManagerName);
            mVisibleRepresentation.AttachTo(this, false);
            mVisibleRepresentation.Visible = false;

            mVisibleRepresentation.ScaleX = 0.1f;
            mVisibleRepresentation.ScaleY = 0.1f;

            mVisibleRepresentation2 = SpriteManager.AddSprite(@"Content\friendBullet", mContentManagerName);
            mVisibleRepresentation2.AttachTo(this, false);
            mVisibleRepresentation2.Visible = false;

            mVisibleRepresentation2.ScaleX = 0.1f;
            mVisibleRepresentation2.ScaleY = 0.1f;

            mCollision = ShapeManager.AddCircle();
            mCollision.AttachTo(this, false);
            mCollision.Radius = 0.1f;
            mCollision.Visible = false;
        }

        public void Show()
        {
            if (IsFriendly)
                mVisibleRepresentation2.Visible = true;
            else
                mVisibleRepresentation.Visible = true;
        }

        public void Reset()
        {
            Velocity.Y = 0.0f;
            mVisibleRepresentation.Visible = false;
            mVisibleRepresentation2.Visible = false;
            IsReady = true;
        }

        public void CheckReady()
        {
            if (Position.Y > 20.0f || Position.Y < -20.0f)
            {
                Reset();
            }
        }

        public virtual void Activity()
        {
            // This code should do things like set Animations, respond to input, and so on.

        }

        public virtual void Destroy()
        {
            // Remove self from the SpriteManager:
            SpriteManager.RemovePositionedObject(this);

            // Remove any other objects you've created:
            SpriteManager.RemoveSprite(mVisibleRepresentation);
            SpriteManager.RemoveSprite(mVisibleRepresentation2);
            ShapeManager.Remove(mCollision);
        }

        #endregion
    }
}
