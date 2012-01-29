using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Input;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Content.AnimationChain;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

// Be sure to replace:
// 1.  The namespace
// 2.  The class name
// 3.  The constructor (should be the same as the class name)


namespace Shmup
{
    public class Player : PositionedObject
    {
        #region Fields

        // Here you'd define things that your Entity contains, like Sprites
        // or Circles:
        private Sprite mVisibleRepresentation;
        private Circle mCollision;

        private List<Sprite> mHealthBarList;
        private Sprite mHealthBar;

        private Sprite[] mIdleA;
        private double mIdlePlayedTime;
        private int mFrameNum;

        private AnimationChainList mAnimations;

        private string mCurrentA;
        private string mLastA;

        private SoundEffect shootSound;

        //private AnimationChainList mPlayerAnimation;

        // Keep the ContentManager for easy access:
        string mContentManagerName;

        float mSpeed = 1.0f;

        public List<Bullet> mAmmo;

        const int MAX_AMMO = 10;
        const double SHOOT_DELAY = 0.5;
        const float BULLET_SPEED = 20.0f;

        double mTimeOfLastShot;

        public int mHealth;

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
        public Player(string contentManagerName)
        {
            // Set the ContentManagerName and call Initialize:
            mContentManagerName = contentManagerName;

            mTimeOfLastShot = TimeManager.CurrentTime;

            mIdleA = new Sprite[2];
            mIdlePlayedTime = TimeManager.CurrentTime;
            mFrameNum = 0;

            mHealthBarList = new List<Sprite>();
            mHealthBarList.Add(SpriteManager.AddSprite(@"Content\HealthBar\health0"));
            mHealthBarList.Add(SpriteManager.AddSprite(@"Content\HealthBar\health1"));
            mHealthBarList.Add(SpriteManager.AddSprite(@"Content\HealthBar\health2"));
            mHealthBarList.Add(SpriteManager.AddSprite(@"Content\HealthBar\health3"));

            mAnimations = new AnimationChainList();
            AnimationChain idle = new AnimationChain();
            idle.Add(new AnimationFrame(@"Content\Player\playeridle0", .12f, "Global"));
            idle.Add(new AnimationFrame(@"Content\Player\playeridle1", .12f, "Global"));

            AnimationChain die = new AnimationChain();
            die.Add(new AnimationFrame(@"Content\Player\playerdie0", .12f, "Global"));
            die.Add(new AnimationFrame(@"Content\Player\playerdie1", .12f, "Global"));
            die.Add(new AnimationFrame(@"Content\Player\playerdie2", .12f, "Global"));

            mAnimations.Add(idle);
            mAnimations.Add(die);
            InitializeIdle();

            mHealth = 3;

            // If you don't want to add to managers, make an overriding constructor
            Initialize(true);
        }

        private void InitializeIdle()
        {
            for (int i = 0; i < mIdleA.Length; i++)
            {
                mIdleA[i] = SpriteManager.AddSprite(@"Content\Player\playeridle" + i, "Global");
                mIdleA[i].AttachTo(this, false);
                mIdleA[i].Visible = false;
                mIdleA[i].ScaleX = 1.4f;
                mIdleA[i].ScaleY = 1.4f;
            }
        }

        protected virtual void Initialize(bool addToManagers)
        {
            // Here you can preload any content you will be using
            // like .scnx files or texture files.

            mAmmo = new List<Bullet>(MAX_AMMO);

            for (int i = 0; i < MAX_AMMO; i++)
            {
                mAmmo.Add(new Bullet(FlatRedBallServices.GlobalContentManager));
            }

            foreach (Bullet b in mAmmo)
            {
                b.IsFriendly = true;
            }

            //mPlayerAnimation = FlatRedBallServices.Load<AnimationChainList>(@"Content\PlayerAnimation", "Global");

            shootSound = FlatRedBallServices.Load<SoundEffect>(@"Content\Sounds\LaserShot");

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
            //mVisibleRepresentation = SpriteManager.AddSprite(@"Content\player", mContentManagerName);
            mVisibleRepresentation = SpriteManager.AddSprite(mAnimations[0]);
            mVisibleRepresentation.AttachTo(this, false);
            mVisibleRepresentation.ScaleX = 1.2f;
            mVisibleRepresentation.ScaleY = 1.2f;
            mVisibleRepresentation.Visible = false;

            mHealthBarList[mHealth].AttachTo(SpriteManager.Camera, false);
            mHealthBarList[mHealth].ScaleX = 3.0f;
            mHealthBarList[mHealth].ScaleY = 1.0f;

            mCollision = ShapeManager.AddCircle();
            mCollision.AttachTo(this, false);
            mCollision.Radius = 0.6f;
            mCollision.Visible = false;

            this.Y = -12.0f;
        }

        private void Animate(string aName)
        {
            if (aName.Equals("Idle"))
            {
                mIdleA[mFrameNum].Visible = true;

                if (TimeManager.CurrentTime - mIdlePlayedTime > 0.6f)
                {
                    mIdleA[mFrameNum].Visible = false;

                    mFrameNum++;

                    if (mFrameNum >= mIdleA.Length)
                        mFrameNum = 0;

                    mIdleA[mFrameNum].Visible = true;
                }
            }
        }

        private void Shoot()
        {
            foreach (Bullet b in mAmmo)
            {
                if (b.IsReady)
                {
                    b.Position.X = this.X;
                    b.Position.Y = this.Y;
                    b.Velocity.Y = BULLET_SPEED;
                    b.Show();
                    b.IsReady = false;
                    mTimeOfLastShot = TimeManager.CurrentTime;
                    shootSound.Play();
                    break;
                }
            }
        }

        public bool IsDead()
        {
            return (mHealth <= 0);
        }

        public virtual void Activity()
        {
            // This code should do things like set Animations, respond to input, and so on.

            if (InputManager.TouchScreen.ScreenDown)
            {
                if (TimeManager.CurrentTime - mTimeOfLastShot > SHOOT_DELAY)
                    Shoot();

                //System.Diagnostics.Debug.WriteLine(InputManager.TouchScreen.X + " - " + this.X);

                if (Math.Abs(InputManager.TouchScreen.WorldXAt(0) - this.X) > 0.6f)
                {
                    if (InputManager.TouchScreen.WorldXAt(0) - this.X > 0)
                    {
                        this.X += mSpeed;
                    }
                    else
                    {
                        this.X -= mSpeed;
                    }
                }

                //MoveTo(x);
            }

            foreach (Bullet b in mAmmo)
            {
                if (!b.IsReady)
                    b.CheckReady();
            }

            Animate("Idle");
        }

        public virtual void Destroy()
        {
            // Remove self from the SpriteManager:
            SpriteManager.RemovePositionedObject(this);

            // Remove any other objects you've created:
            SpriteManager.RemoveSprite(mVisibleRepresentation);
            ShapeManager.Remove(mCollision);

            for (int i = 0; i < mIdleA.Length; i++)
            {
                SpriteManager.RemoveSprite(mIdleA[i]);
            }

            foreach (Bullet b in mAmmo)
            {
                b.Destroy();
            }
        }

        #endregion
    }
}
