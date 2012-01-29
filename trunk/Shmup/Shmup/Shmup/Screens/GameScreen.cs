using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Input;
using FlatRedBall.IO;
using FlatRedBall.Broadcasting;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Audio;

namespace Shmup.Screens
{
    public class GameScreen : Screen
    {

        Player player1;
        List<Shooter> enemies;
        List<Shooter> friendlies;

        Text waveText;
        //Text winText;
        Text numEnemies;
        Text numAllies;

        SoundEffect explosionSound;

        Sprite bg;

        double timeOfLastSpawn;
        const double SPAWN_DELAY = 1.0f;
	
        #region Methods

        #region Constructor and Initialize

        public GameScreen() : base("GameScreen")
        {
            // Don't put initialization code here, do it in
            // the Initialize method below
            //   |   |   |   |   |   |   |   |   |   |   |
            //   |   |   |   |   |   |   |   |   |   |   |
            //   V   V   V   V   V   V   V   V   V   V   V

        }

        public override void Initialize(bool addToManagers)
        {
            // Set the screen up here instead of in the Constructor to avoid
            // exceptions occurring during the constructor.

            player1 = new Player(FlatRedBallServices.GlobalContentManager);

            friendlies = new List<Shooter>();
            enemies = new List<Shooter>();

            for (int i = 0; i < WaveProperties.WaveNumEnemies; i++)
            {
                enemies.Add(new Shooter(FlatRedBallServices.GlobalContentManager));
            }

            timeOfLastSpawn = TimeManager.CurrentTime;

            explosionSound = FlatRedBallServices.Load<SoundEffect>(@"Content\Sounds\Explosion");

			
			// AddToManagers should be called LAST in this method:
			if(addToManagers)
			{
				AddToManagers();
			}
        }

		public override void AddToManagers()
        {
            waveText = TextManager.AddText("Wave " + WaveProperties.WaveNumber);
            waveText.Scale = 2.0f;
            waveText.Spacing = 2.0f;
            waveText.X = -5.0f;

            numEnemies = TextManager.AddText("");
            numEnemies.Scale = 1.5f;
            numEnemies.Spacing = 1.5f;
            numEnemies.X = -9.0f;
            numEnemies.Y = 15.0f;
            numEnemies.Red = 0.1f;

            numAllies = TextManager.AddText("");
            numAllies.Scale = 1.5f;
            numAllies.Spacing = 1.5f;
            numAllies.X = 7.0f;
            numAllies.Y = 15.0f;
            numAllies.Green = 0.1f;

            /*
            winText = TextManager.AddText("YOU WIN");
            winText.Scale = 2.0f;
            winText.Spacing = 2.0f;
            winText.X = -8.0f;
            winText.Visible = false;
             * */

            bg = SpriteManager.AddSprite(@"Content\BG\bg");
            bg.ScaleX = 20.0f;
            bg.ScaleY = 20.0f;
            bg.Z = -1.0f;

		}
		
        #endregion

        private void SpawnEnemy()
        {
            foreach (Shooter s in enemies)
            {
                if (!s.Spawned)
                {
                    s.Position.Y = 15.0f;
                    s.Position.X = FlatRedBallServices.Random.Next(-5, 5);
                    //Add code so enemy moves to position in y then stops and just shoots
                    s.Velocity.Y = -10.0f;
                    s.ShowEnemy();
                    s.Spawned = true;
                    break;
                }
            }
        }

        private void CheckCollisions()
        {
            List<Shooter> newEnemies = new List<Shooter>();
            List<Shooter> newFriendlies = new List<Shooter>();

            foreach (Shooter e in enemies)
            {
                foreach (Bullet b in e.mAmmo)
                {
                    if (!b.IsReady)
                    {
                        foreach (Shooter f in friendlies)
                        {
                            if (b.Collision.CollideAgainst(f.Collision) && f.IsVisible())
                            {
                                f.Convert();
                                b.Reset();
                                //f.SwarmPoint = new Vector3(0.0f, 10.0f, 0.0f);
                                explosionSound.Play();

                                if (!newEnemies.Contains(f))
                                    newEnemies.Add(f);
                            }
                        }

                        if (b.Collision.CollideAgainst(player1.Collision))
                        {
                            player1.mHealth--;
                            b.Reset();
                        }
                    }
                }
            }

            foreach (Shooter f in friendlies)
            {
                foreach (Bullet b in f.mAmmo)
                {
                    foreach (Shooter e in enemies)
                    {
                        if (!b.IsReady && b.Collision.CollideAgainst(e.Collision) && e.IsVisible())
                        {
                            e.Convert();
                            b.Reset();
                            //e.SwarmPoint = player1.Position;
                            explosionSound.Play();

                            if (!newFriendlies.Contains(e))
                                newFriendlies.Add(e);
                        }
                    }
                }
            }

            foreach (Bullet b in player1.mAmmo)
            {
                foreach (Shooter e in enemies)
                {
                    if (!b.IsReady && b.Collision.CollideAgainst(e.Collision) && e.IsVisible())
                    {
                        e.Convert();
                        b.Reset();
                        explosionSound.Play();

                        if (!newFriendlies.Contains(e))
                            newFriendlies.Add(e);
                    }
                }
            }

            foreach (Shooter e in enemies)
            {
                if (!newFriendlies.Contains(e))
                    newEnemies.Add(e);
            }

            foreach (Shooter f in friendlies)
            {
                if (!newEnemies.Contains(f))
                    newFriendlies.Add(f);
            }

            enemies = newEnemies;
            friendlies = newFriendlies;

            foreach (Shooter e in enemies)
            {
                e.SwarmPoint = new Vector3(0.0f, 10.0f, 0.0f);
            }

            foreach (Shooter f in friendlies)
            {
                f.SwarmPoint = player1.Position;
            }
        }

        #region Public Methods

        public override void Activity(bool firstTimeCalled)
        {
            base.Activity(firstTimeCalled);

            if (enemies.Count == 0)
            {
                //System.Diagnostics.Debug.WriteLine("YOU WIN");
                //winText.Visible = true;

                WaveProperties.WaveNumEnemies += 5;
                WaveProperties.WaveNumber += 1;

                this.Destroy();

                MoveToScreen(typeof(GameScreen).FullName);
            }

            player1.Activity();
            CheckCollisions();

            if (TimeManager.CurrentTime - timeOfLastSpawn > SPAWN_DELAY)
            {
                SpawnEnemy();
                waveText.Visible = false;
                timeOfLastSpawn = TimeManager.CurrentTime;
            }

            foreach (Shooter s in enemies)
            {
                s.Activity();
            }

            foreach (Shooter s in friendlies)
            {
                s.Activity();
            }

            numAllies.DisplayText = "" + friendlies.Count;
            numEnemies.DisplayText = "" + enemies.Count;

            // Look up how accelerometer works
            
        }

        public override void Destroy()
        {
            base.Destroy();

            player1.Destroy();

            foreach (Shooter e in enemies)
            {
                e.Destroy();
            }

            foreach (Shooter f in friendlies)
            {
                f.Destroy();
            }

            TextManager.RemoveText(waveText);
            TextManager.RemoveText(numEnemies);
            TextManager.RemoveText(numAllies);
            SpriteManager.RemoveSprite(bg);

        }

        #endregion

		
        #endregion
    }
}

