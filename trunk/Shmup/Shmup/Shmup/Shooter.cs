using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Math.Geometry;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

// Be sure to replace:
// 1.  The namespace
// 2.  The class name
// 3.  The constructor (should be the same as the class name)


namespace Shmup
{
    public class Shooter : PositionedObject
    {
        #region Fields

        // Here you'd define things that your Entity contains, like Sprites
        // or Circles:
        private Sprite mVisibleRepresentation;
        private Sprite mVisibleRepresentation2;
        private Circle mCollision;

        // Keep the ContentManager for easy access:
        string mContentManagerName;

        private SoundEffect shootSound;

        public bool Friendly;
        public bool Spawned;

        public List<Bullet> mAmmo;

        const int MAX_AMMO = 10;
        const double SHOOT_DELAY = 1.0;
        const float BULLET_SPEED = 20.0f;

        double mTimeOfLastShot;

        public Vector3 SwarmPoint;

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
        public Shooter(string contentManagerName)
        {
            // Set the ContentManagerName and call Initialize:
            mContentManagerName = contentManagerName;

            Friendly = false;
            Spawned = false;
            mTimeOfLastShot = TimeManager.CurrentTime;

            SwarmPoint = new Vector3(0.0f, 10.0f, 0.0f);

            // If you don't want to add to managers, make an overriding constructor
            Initialize(true);
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
            mVisibleRepresentation = SpriteManager.AddSprite(@"Content\enemy", mContentManagerName);
            mVisibleRepresentation.AttachTo(this, false);
            mVisibleRepresentation.Visible = false;
            mVisibleRepresentation.ScaleX = 0.7f;
            mVisibleRepresentation.ScaleY = 0.7f;

            mVisibleRepresentation2 = SpriteManager.AddSprite(@"Content\ally", mContentManagerName);
            mVisibleRepresentation2.AttachTo(this, false);
            mVisibleRepresentation2.Visible = false;
            mVisibleRepresentation2.ScaleX = 0.7f;
            mVisibleRepresentation2.ScaleY = 0.7f;

            mCollision = ShapeManager.AddCircle();
            mCollision.AttachTo(this, false);
            //mCollision.Visible = false;
            mCollision.Radius = 0.5f;
        }

        private void Shoot()
        {
            foreach (Bullet b in mAmmo)
            {
                if (b.IsReady)
                {
                    b.Position.X = this.X;
                    b.Position.Y = this.Y;

                    if (Friendly)
                        b.Velocity.Y = BULLET_SPEED;
                    else
                        b.Velocity.Y = -BULLET_SPEED;

                    b.IsFriendly = Friendly;
                    b.Show();
                    b.IsReady = false;
                    mTimeOfLastShot = TimeManager.CurrentTime;
                    shootSound.Play();
                    break;
                }
            }
        }

        public void Convert()
        {
            if (Friendly)
            {
                Friendly = false;

                mVisibleRepresentation.Visible = false;
                mVisibleRepresentation2.Visible = false;

                mCollision.Radius = 0.5f;

                Spawned = false;
            }
            else
            {
                Friendly = true;

                Position.Y = -15.0f;
                Position.X = FlatRedBallServices.Random.Next(-10, 10);

                Velocity.Y = 0.0f;

                mCollision.Radius = 0.45f;

                ShowFriend();
            }
        }

        public void ShowEnemy()
        {
            mVisibleRepresentation.Visible = true;
            mVisibleRepresentation2.Visible = false;
        }

        public void ShowFriend()
        {
            mVisibleRepresentation2.Visible = true;
            mVisibleRepresentation.Visible = false;
        }

        public bool IsVisible()
        {
            return mVisibleRepresentation.Visible || mVisibleRepresentation2.Visible;
        }

        private void Orbit()
        {
            Acceleration.X = MathHelper.Clamp(SwarmPoint.X - Position.X, -50.0f, 50.0f);
            Acceleration.Y = MathHelper.Clamp(SwarmPoint.Y - Position.Y, -50.0f, 50.0f);

            //System.Diagnostics.Debug.WriteLine(SwarmPoint.X + ", " + SwarmPoint.Y);
        }

        public virtual void Activity()
        {
            // This code should do things like set Animations, respond to input, and so on.

            if (TimeManager.CurrentTime - mTimeOfLastShot > SHOOT_DELAY && IsVisible())
                Shoot();

            foreach (Bullet b in mAmmo)
            {
                if (!b.IsReady)
                    b.CheckReady();
            }

            Orbit();
            
            if (Velocity.X > 10.0f)
                Velocity.X = 10.0f;
            else if (Velocity.X < -10.0f)
                Velocity.X = -10.0f;

            if (Velocity.Y > 10.0f)
                Velocity.Y = 10.0f;
            else if (Velocity.Y < -10.0f)
                Velocity.Y = -10.0f;
            
        }

        public virtual void Destroy()
        {
            // Remove self from the SpriteManager:
            SpriteManager.RemovePositionedObject(this);

            // Remove any other objects you've created:
            SpriteManager.RemoveSprite(mVisibleRepresentation);
            SpriteManager.RemoveSprite(mVisibleRepresentation2);
            ShapeManager.Remove(mCollision);

            foreach (Bullet b in mAmmo)
            {
                b.Destroy();
            }
        }

        #endregion
    }
}

