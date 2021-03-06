﻿using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;

namespace Sharpshooter.Champions
{
    public static class Teemo
    {
        static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        static Orbwalking.Orbwalker Orbwalker { get { return SharpShooter.Orbwalker; } }

        static Spell Q, W, E, R;

        public static void Load()
        {
            Q = new Spell(SpellSlot.Q, 680f);
            W = new Spell(SpellSlot.W);
            R = new Spell(SpellSlot.R, 230f);

            R.SetSkillshot(0.25f, 75f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            SharpShooter.Menu.SubMenu("Combo").AddItem(new MenuItem("comboUseQ", "使用 Q", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Combo").AddItem(new MenuItem("comboUseW", "使用 W", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Combo").AddItem(new MenuItem("comboUseR", "使用 R", true).SetValue(true));

            SharpShooter.Menu.SubMenu("Harass").AddItem(new MenuItem("harassUseQ", "使用 Q", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Harass").AddItem(new MenuItem("harassMana", "蓝量 % >", true).SetValue(new Slider(50, 0, 100)));

            SharpShooter.Menu.SubMenu("Laneclear").AddItem(new MenuItem("empty1", "空的", true));

            SharpShooter.Menu.SubMenu("Jungleclear").AddItem(new MenuItem("jungleclearUseQ", "使用 Q", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Jungleclear").AddItem(new MenuItem("jungleclearMana", "蓝量 % >", true).SetValue(new Slider(20, 0, 100)));

            SharpShooter.Menu.SubMenu("Misc").AddItem(new MenuItem("antigapcloser", "使用防突进", true).SetValue(true));

            SharpShooter.Menu.SubMenu("Drawings").AddItem(new MenuItem("drawingAA", "真实 AA 范围", true).SetValue(new Circle(true, Color.FromArgb(171, 242, 0))));
            SharpShooter.Menu.SubMenu("Drawings").AddItem(new MenuItem("drawingQ", "Q 范围", true).SetValue(new Circle(true, Color.FromArgb(171, 242, 0))));
            SharpShooter.Menu.SubMenu("Drawings").AddItem(new MenuItem("drawingR", "R 范围", true).SetValue(new Circle(false, Color.FromArgb(171, 242, 0))));

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Orbwalking.AfterAttack += Orbwalking_OnAfterAttack;
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead)
                return;

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                Combo();

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                Harass();

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                Laneclear();
                Jungleclear();
            }
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead)
                return;

            var drawingAA = SharpShooter.Menu.Item("drawingAA", true).GetValue<Circle>();
            var drawingQ = SharpShooter.Menu.Item("drawingQ", true).GetValue<Circle>();
            var drawingR = SharpShooter.Menu.Item("drawingR", true).GetValue<Circle>();

            if (drawingAA.Active)
                Render.Circle.DrawCircle(Player.Position, Orbwalking.GetRealAutoAttackRange(Player), drawingAA.Color);

            if (drawingQ.Active && Q.IsReady())
                Render.Circle.DrawCircle(Player.Position, Q.Range, drawingQ.Color);

            if (drawingR.Active && R.IsReady())
                Render.Circle.DrawCircle(Player.Position, R.Range, drawingR.Color);
        }

        static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!SharpShooter.Menu.Item("antigapcloser", true).GetValue<Boolean>() || Player.IsDead)
                return;

            if (gapcloser.Sender.IsValidTarget(1000))
            {
                Render.Circle.DrawCircle(gapcloser.Sender.Position, gapcloser.Sender.BoundingRadius, Color.Gold, 5);
                var targetpos = Drawing.WorldToScreen(gapcloser.Sender.Position);
                Drawing.DrawText(targetpos[0] - 40, targetpos[1] + 20, Color.Gold, "Gapcloser");
            }

            if (Q.CanCast(gapcloser.Sender))
                Q.Cast(gapcloser.Sender);

            if (W.IsReady())
                W.Cast();
        }

        static void Orbwalking_OnAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe || !(target.Type == GameObjectType.obj_AI_Hero))
                return;

            if(!Q.CanCast((Obj_AI_Hero)target))
                return;

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                if (SharpShooter.Menu.Item("comboUseQ", true).GetValue<Boolean>())
                    Q.Cast((Obj_AI_Hero)target);
            }
            else
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                if (SharpShooter.Menu.Item("harassUseQ", true).GetValue<Boolean>())
                    Q.Cast((Obj_AI_Hero)target);
            }

        }

        static void Combo()
        {
            if (!Orbwalking.CanMove(1))
                return;

            if (SharpShooter.Menu.Item("comboUseQ", true).GetValue<Boolean>() & Q.IsReady())
            {
                var Qtarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical, true);

                if (Q.CanCast(Qtarget) && !Qtarget.IsValidTarget(670))
                    Q.Cast(Qtarget);
            }

            if (SharpShooter.Menu.Item("comboUseW", true).GetValue<Boolean>() & W.IsReady())
            {
                var Wtarget = HeroManager.Enemies.Where(x => x.IsValidTarget(1000) && !x.IsFacing(Player)).FirstOrDefault();

                if (Wtarget != null)
                    W.Cast();
            }

            if (SharpShooter.Menu.Item("comboUseR", true).GetValue<Boolean>() & R.IsReady())
            {
                var Rtarget = HeroManager.Enemies.Where(x => R.CanCast(x) && !x.HasBuff("bantamtraptarget", true)).OrderBy(x => x.Distance(Player.Position, false)).FirstOrDefault();

                if (R.CanCast(Rtarget))
                    R.Cast(Rtarget);
            }
        }

        static void Harass()
        {
            if (!Orbwalking.CanMove(1) || !(Player.ManaPercentage() > SharpShooter.Menu.Item("harassMana", true).GetValue<Slider>().Value))
                return;

            if (SharpShooter.Menu.Item("harassUseQ", true).GetValue<Boolean>() & Q.IsReady())
            {
                var Qtarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical, true);

                if (Q.CanCast(Qtarget) && !Qtarget.IsValidTarget(670))
                    Q.Cast(Qtarget);
            }
        }

        static void Laneclear()
        {
            var bigminion = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Enemy).Where(x => Q.CanCast(x) && x.Health <= Q.GetDamage(x) && (x.SkinName.ToLower().Contains("siege") || x.SkinName.ToLower().Contains("super"))).FirstOrDefault();

            if (Q.CanCast(bigminion))
                Q.Cast(bigminion);
        }

        static void Jungleclear()
        {
            if (!Orbwalking.CanMove(1) || !(Player.ManaPercentage() > SharpShooter.Menu.Item("jungleclearMana", true).GetValue<Slider>().Value))
                return;

            var Mobs = MinionManager.GetMinions(Player.ServerPosition, Orbwalking.GetRealAutoAttackRange(Player) + 100, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (Mobs.Count <= 0)
                return;

            if (SharpShooter.Menu.Item("jungleclearUseQ", true).GetValue<Boolean>() && Q.IsReady())
            {
                if (Q.CanCast(Mobs[0]))
                    Q.Cast(Mobs[0]);
            }
        }
    }
}
