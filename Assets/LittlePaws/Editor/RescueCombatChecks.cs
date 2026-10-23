using System;
using LittlePaws.Rescue;
using UnityEditor;
using UnityEngine;

namespace LittlePaws.Rescue
{
public static class RescueCombatChecks
{
    [MenuItem("Little Paws/Check Rescue the Egg combat")]
    public static void Run()
    {
        var game = new RescueCombat();
        Require(game.HeroHealth == 50 && game.TimeRemaining == 7200 && game.Enemies.Count == 1, "Starting health, timer and test enemy");
        game.Tick(.1f, Vector2.left);
        Require(game.HeroPosition.x == 0 && game.HeroWalking && game.Facing == -1, "Walk animation and facing at left boundary");
        game.Tick(.1f, Vector2.up);
        Require(game.Facing == -1, "Vertical movement preserves facing");
        game.HeroPosition = new Vector2(15, 4);
        game.Tick(.1f, Vector2.one);
        Require(game.HeroPosition.x == 15 && game.HeroPosition.y == 4 && game.StageIndex == 0, "Uncleared stage and head boundaries");

        game = new RescueCombat();
        var enemy = game.Enemies[0];
        enemy.Position = game.HeroPosition + Vector2.right * .3f;
        Require(game.TryAttack() && enemy.Health == 29 && game.LastSlashDirection == -1, "First tap damages one and swings left");
        game.Tick(.2f, Vector2.up);
        Require(game.HeroPosition.y == 2 && game.HeroHealth == 50 && enemy.Position.x > .3f, "Combo blocks vertical movement; damage prevents retaliation and knocks back");
        Require(game.TryAttack() && enemy.Health == 28 && game.LastSlashDirection == 1, "Second tap swings right and refreshes stun");
        game.Tick(.31f, Vector2.zero);
        Require(enemy.Mode == RescueEnemyMode.Damaged, "Each new hit extends damage mode");
        game.Tick(.2f, Vector2.zero);
        Require(enemy.Mode == RescueEnemyMode.Lying && !game.TryAttack(), "Recovered enemy lies invulnerable");
        game.Tick(1.8f, Vector2.zero);
        Require(enemy.Mode == RescueEnemyMode.Lying, "Lying lasts two seconds");
        game.Tick(.25f, Vector2.zero);
        Require(enemy.Mode != RescueEnemyMode.Lying, "Enemy recovers after two seconds");

        game = new RescueCombat();
        enemy = game.Enemies[0];
        enemy.Position = game.HeroPosition;
        game.Tick(1.01f, Vector2.zero);
        Require(game.HeroHealth == 50 && enemy.AttackSerial == 1 && enemy.AttackAge < .03f, "Enemy sword animation starts before damage");
        game.Tick(.22f, Vector2.zero);
        Require(game.HeroHealth == 30, "Uninterrupted enemy sword deals twenty damage after windup");
        game.Tick(2.1f, Vector2.zero);
        Require(game.HeroHealth == 0 && game.IsGameOver, "Three enemy attacks end the game");

        game = new RescueCombat();
        enemy = game.Enemies[0];
        enemy.Position = game.HeroPosition;
        game.Tick(1.01f, Vector2.zero);
        Require(game.TryAttack() && enemy.Mode == RescueEnemyMode.Damaged, "Hit interrupts a pending enemy swing");
        game.Tick(.49f, Vector2.zero);
        Require(game.HeroHealth == 50 && game.HeroDamageSerial == 0 && enemy.AttackAge >= 10f, "Damage mode cancels enemy animation and pending hero damage");
        game.Tick(2.05f, Vector2.zero);
        enemy.Position = game.HeroPosition;
        game.Tick(.5f, Vector2.zero);
        Require(game.HeroHealth == 50 && enemy.AttackSerial == 1, "Recovered enemy must wait before starting another attack");
        game.Tick(.75f, Vector2.zero);
        Require(game.HeroHealth == 30, "Recovered enemy can damage hero after a fresh cooldown and swing");

        game = new RescueCombat();
        enemy = game.Enemies[0];
        enemy.Position = enemy.PatrolOrigin = new Vector2(10, 2);
        game.Tick(8, Vector2.zero);
        Require(enemy.Mode == RescueEnemyMode.Patrol && enemy.Position.x >= 7.5f && enemy.Position.x <= 12.5f, "Undetected enemy patrols within five-unit span");
        enemy.Position = game.HeroPosition = new Vector2(0, 2);
        game.Tick(.01f, Vector2.left);
        Require(game.TryAttack(), "Left-facing overlapping hit connects");
        game.Tick(.51f, Vector2.zero);
        Require(enemy.Position.x < 0 && enemy.Mode == RescueEnemyMode.Lying, "Knockback can leave left stage edge");
        game.HeroPosition = new Vector2(10, 2);
        game.Tick(2.6f, Vector2.zero);
        Require(enemy.Position.x >= 0 && enemy.Mode == RescueEnemyMode.Returning, "Recovered enemy returns inside stage before resuming patrol");

        game = new RescueCombat();
        for (int stage = 0; stage < 3; stage++)
        {
            enemy = game.Enemies[0];
            enemy.Position = game.HeroPosition;
            for (int hit = 0; hit < 30; hit++) Require(game.TryAttack(), "Thirty button presses kill thirty-health enemy");
            Require(game.StageCleared && enemy.Health == 0 && !game.TryAttack(), "Dead enemy cannot take more damage");
            game.HeroPosition = new Vector2(game.StageEnd, 2);
            game.Tick(.05f, Vector2.right);
            Require(stage == 2 ? game.IsVictory : game.StageIndex == stage + 1, "Stage requires physically crossing cleared right edge");
        }
        game = new RescueCombat();
        game.Enemies[0].Health = 0;
        for (int second = 0; second < 60; second++)
            for (int frame = 0; frame < 60; frame++) game.Tick(1f / 60, Vector2.zero);
        Require(Mathf.Abs(game.TimeRemaining - 7140) < .001f, "Countdown tracks sixty seconds of frame updates accurately");
        game.Tick(7201, Vector2.zero);
        Require(game.TimeRemaining == 0 && game.IsGameOver && !game.IsVictory, "Two-hour timeout ends run");
        Debug.Log("Rescue combat checks passed: bounds, facing, combo timing, knockback, recovery, enemy attacks, stages, victory and timeout.");
    }

    static void Require(bool condition, string reason)
    {
        if (!condition) throw new Exception("Rescue combat: " + reason);
    }
}
}
