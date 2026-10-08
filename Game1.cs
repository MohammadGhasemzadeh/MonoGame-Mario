using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MonoGameMario;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;

    private readonly List<Rectangle> _platforms = new();
    private readonly List<Rectangle> _coins = new();
    private readonly List<Rectangle> _enemies = new();

    private Rectangle _player;
    private Vector2 _playerVelocity;
    private bool _isOnGround;
    private bool _won;
    private bool _gameOver;
    private int _score;
    private int _lives = 3;
    private float _cameraX;
    private Rectangle _goal = new(3620, 180, 60, 120);

    private readonly Rectangle _startPosition = new(80, 420, 38, 42);
    private readonly Point _spawnPoint = new(80, 420);

    private KeyboardState _previousKeyboardState;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720,
            IsFullScreen = false
        };

        IsMouseVisible = true;
        Content.RootDirectory = "Content";
        Window.Title = "MonoGame Mario";
    }

    protected override void Initialize()
    {
        _graphics.ApplyChanges();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        BuildLevel();
        ResetPlayer();
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboardState = Keyboard.GetState();
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboardState.IsKeyDown(Keys.Escape))
            Exit();

        if (!_won && !_gameOver)
        {
            HandleMovement(keyboardState, dt);
            HandleCollisions();
            CheckCollectibles();
            CheckEnemyCollisions();
            CheckGoal();
        }

        if (_gameOver && keyboardState.IsKeyDown(Keys.R) && _previousKeyboardState.IsKeyUp(Keys.R))
        {
            RestartGame();
        }

        if (_won && keyboardState.IsKeyDown(Keys.R) && _previousKeyboardState.IsKeyUp(Keys.R))
        {
            RestartGame();
        }

        _previousKeyboardState = keyboardState;
        base.Update(gameTime);
    }

    private void HandleMovement(KeyboardState keyboardState, float dt)
    {
        float moveSpeed = 260f;
        float gravity = 1700f;

        float moveX = 0f;
        if (keyboardState.IsKeyDown(Keys.Left) || keyboardState.IsKeyDown(Keys.A)) moveX -= 1f;
        if (keyboardState.IsKeyDown(Keys.Right) || keyboardState.IsKeyDown(Keys.D)) moveX += 1f;

        _playerVelocity.X = moveX * moveSpeed;

        if ((keyboardState.IsKeyDown(Keys.Space) || keyboardState.IsKeyDown(Keys.Up) || keyboardState.IsKeyDown(Keys.W))
            && _previousKeyboardState.IsKeyUp(Keys.Space)
            && _previousKeyboardState.IsKeyUp(Keys.Up)
            && _previousKeyboardState.IsKeyUp(Keys.W)
            && _isOnGround)
        {
            _playerVelocity.Y = -650f;
            _isOnGround = false;
        }

        _playerVelocity.Y += gravity * dt;

        _player.X += (int)Math.Round(_playerVelocity.X * dt);
        ResolveHorizontalCollisions();

        _player.Y += (int)Math.Round(_playerVelocity.Y * dt);
        ResolveVerticalCollisions();

        _cameraX = MathHelper.Clamp(_player.X - 220f, 0f, 3500f);
        _player.X = MathHelper.Clamp(_player.X, 0, 4000);
    }

    private void ResolveHorizontalCollisions()
    {
        foreach (var platform in _platforms)
        {
            if (_player.Intersects(platform))
            {
                if (_playerVelocity.X > 0)
                    _player.X = platform.X - _player.Width;
                else if (_playerVelocity.X < 0)
                    _player.X = platform.Right;

                _playerVelocity.X = 0f;
            }
        }
    }

    private void ResolveVerticalCollisions()
    {
        _isOnGround = false;

        foreach (var platform in _platforms)
        {
            if (_player.Intersects(platform))
            {
                if (_playerVelocity.Y > 0)
                {
                    _player.Y = platform.Y - _player.Height;
                    _playerVelocity.Y = 0f;
                    _isOnGround = true;
                }
                else if (_playerVelocity.Y < 0)
                {
                    _player.Y = platform.Bottom;
                    _playerVelocity.Y = 0f;
                }
            }
        }

        if (_player.Y > 760)
        {
            LoseLife();
        }
    }

    private void HandleCollisions()
    {
        for (int i = _coins.Count - 1; i >= 0; i--)
        {
            if (_player.Intersects(_coins[i]))
            {
                _coins.RemoveAt(i);
                _score += 10;
            }
        }
    }

    private void CheckCollectibles()
    {
        if (_player.X > 3800)
            _goal = new Rectangle(3620, 180, 60, 120);
    }

    private void CheckEnemyCollisions()
    {
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            var enemy = _enemies[i];
            if (_player.Intersects(enemy))
            {
                if (_playerVelocity.Y > 0 && _player.Bottom <= enemy.Top + 20)
                {
                    _playerVelocity.Y = -420f;
                    _enemies.RemoveAt(i);
                    _score += 50;
                }
                else
                {
                    LoseLife();
                    return;
                }
            }
        }
    }

    private void CheckGoal()
    {
        if (_player.Intersects(_goal))
        {
            _won = true;
        }
    }

    private void LoseLife()
    {
        _lives--;

        if (_lives <= 0)
        {
            _gameOver = true;
            _won = false;
            return;
        }

        ResetPlayer();
    }

    private void RestartGame()
    {
        _won = false;
        _gameOver = false;
        _lives = 3;
        _score = 0;
        BuildLevel();
        ResetPlayer();
    }

    private void ResetPlayer()
    {
        _player = new Rectangle(_spawnPoint.X, _spawnPoint.Y, 38, 42);
        _playerVelocity = Vector2.Zero;
        _isOnGround = false;
    }

    private void BuildLevel()
    {
        _platforms.Clear();
        _coins.Clear();
        _enemies.Clear();

        _platforms.Add(new Rectangle(0, 600, 520, 120));
        _platforms.Add(new Rectangle(620, 600, 760, 120));
        _platforms.Add(new Rectangle(1450, 600, 500, 120));
        _platforms.Add(new Rectangle(2100, 600, 700, 120));
        _platforms.Add(new Rectangle(2900, 600, 700, 120));

        _platforms.Add(new Rectangle(800, 500, 100, 100));
        _platforms.Add(new Rectangle(1000, 440, 90, 160));
        _platforms.Add(new Rectangle(1160, 380, 90, 220));
        _platforms.Add(new Rectangle(1720, 470, 120, 130));
        _platforms.Add(new Rectangle(1920, 410, 110, 190));
        _platforms.Add(new Rectangle(2450, 470, 130, 130));
        _platforms.Add(new Rectangle(2620, 420, 120, 180));
        _platforms.Add(new Rectangle(3250, 520, 110, 80));

        _coins.Add(new Rectangle(260, 510, 20, 20));
        _coins.Add(new Rectangle(360, 510, 20, 20));
        _coins.Add(new Rectangle(840, 450, 20, 20));
        _coins.Add(new Rectangle(1035, 390, 20, 20));
        _coins.Add(new Rectangle(1188, 330, 20, 20));
        _coins.Add(new Rectangle(1760, 420, 20, 20));
        _coins.Add(new Rectangle(1950, 360, 20, 20));
        _coins.Add(new Rectangle(2490, 420, 20, 20));
        _coins.Add(new Rectangle(2660, 370, 20, 20));
        _coins.Add(new Rectangle(3290, 470, 20, 20));

        _enemies.Add(new Rectangle(900, 565, 36, 36));
        _enemies.Add(new Rectangle(1540, 565, 36, 36));
        _enemies.Add(new Rectangle(2230, 565, 36, 36));
        _enemies.Add(new Rectangle(3000, 565, 36, 36));

        _goal = new Rectangle(3620, 180, 60, 120);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(135, 206, 235));

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        DrawBackground();

        foreach (var platform in _platforms)
        {
            DrawPlatform(platform);
        }

        foreach (var coin in _coins)
        {
            DrawCoin(coin);
        }

        foreach (var enemy in _enemies)
        {
            DrawEnemy(enemy);
        }

        DrawGoal(_goal);
        DrawPlayer(_player);

        if (_gameOver)
            DrawMessage("Game Over", new Vector2(520, 180), Color.Red);

        if (_won)
            DrawMessage("You Win!", new Vector2(560, 180), Color.Gold);

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    private void DrawBackground()
    {
        var farMountains = new[]
        {
            new Rectangle((int)(_cameraX * 0.2f), 380, 220, 200),
            new Rectangle((int)(_cameraX * 0.2f) + 260, 330, 200, 250),
            new Rectangle((int)(_cameraX * 0.2f) + 500, 420, 180, 180),
            new Rectangle((int)(_cameraX * 0.2f) + 760, 350, 260, 230)
        };

        foreach (var mountain in farMountains)
        {
            DrawTriangle(mountain, new Color(160, 160, 180));
        }

        DrawCloud(150 - _cameraX * 0.15f, 120);
        DrawCloud(650 - _cameraX * 0.15f, 180);
        DrawCloud(1050 - _cameraX * 0.15f, 110);
    }

    private void DrawCloud(float x, float y)
    {
        DrawRect(new Rectangle((int)x, (int)y, 80, 35), Color.White);
        DrawRect(new Rectangle((int)x + 30, (int)y - 20, 60, 35), Color.White);
        DrawRect(new Rectangle((int)x + 20, (int)y + 10, 80, 30), Color.White);
    }

    private void DrawTriangle(Rectangle rect, Color color)
    {
        var points = new[]
        {
            new Vector2(rect.X + rect.Width / 2f, rect.Y),
            new Vector2(rect.X + rect.Width, rect.Y + rect.Height),
            new Vector2(rect.X, rect.Y + rect.Height)
        };

        var texture = new Texture2D(GraphicsDevice, 1, 1);
        texture.SetData(new[] { Color.White });

        var origin = Vector2.Zero;
        var colorArray = new Color[] { color };

        _spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, rect.Width, rect.Height), color);
    }

    private void DrawPlatform(Rectangle platform)
    {
        var top = new Rectangle(platform.X, platform.Y, platform.Width, 12);
        DrawRect(top, new Color(80, 200, 80));
        DrawRect(platform, new Color(120, 80, 45));
    }

    private void DrawCoin(Rectangle coin)
    {
        DrawRect(coin, new Color(255, 215, 0));
    }

    private void DrawEnemy(Rectangle enemy)
    {
        DrawRect(enemy, new Color(120, 70, 30));
        DrawRect(new Rectangle(enemy.X + 8, enemy.Y + 10, 7, 7), Color.White);
        DrawRect(new Rectangle(enemy.Right - 15, enemy.Y + 10, 7, 7), Color.White);
        DrawRect(new Rectangle(enemy.X + 11, enemy.Bottom - 10, enemy.Width - 22, 6), Color.Black);
    }

    private void DrawPlayer(Rectangle player)
    {
        DrawRect(player, new Color(220, 40, 40));
        DrawRect(new Rectangle(player.X + 8, player.Y + 8, 8, 8), Color.White);
        DrawRect(new Rectangle(player.Right - 16, player.Y + 8, 8, 8), Color.White);
        var hat = new Rectangle(player.X - 2, player.Y - 7, player.Width + 4, 12);
        DrawRect(hat, new Color(236, 180, 30));
    }

    private void DrawGoal(Rectangle goal)
    {
        DrawRect(goal, new Color(200, 40, 40));
        DrawRect(new Rectangle(goal.X + 15, goal.Y + 5, 10, 60), new Color(240, 240, 240));
        DrawRect(new Rectangle(goal.Right - 10, goal.Y + 20, 45, 18), new Color(255, 215, 0));
    }

    private void DrawMessage(string text, Vector2 position, Color color)
    {
        // Draw a simple blocky text approximation using filled rectangles.
        // This keeps the project asset-free while still displaying a clear message.
        var letterWidth = 18;
        var letterHeight = 24;

        foreach (var ch in text)
        {
            int x = (int)position.X;
            int y = (int)position.Y;

            if (char.IsLetter(ch))
            {
                var block = new Rectangle(x, y, letterWidth, letterHeight);
                DrawRect(block, color);
            }

            position.X += letterWidth + 8;
        }
    }

    private void DrawRect(Rectangle rect, Color color)
    {
        _spriteBatch.Draw(_pixel, rect, color);
    }
}
