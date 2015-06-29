﻿#region
using System;
using System.Collections.Generic;
using Colors = System.Drawing.Color;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;
#endregion

namespace Ezreal
{
    internal class Program
    {
        public static string ChampionName = "Ezreal";
        public static Orbwalking.Orbwalker Orbwalker;
		public static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static Menu Config;

        private static void Main()
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != ChampionName) return;
			
            Q = new Spell(SpellSlot.Q, 1150f);
            W = new Spell(SpellSlot.W, 1000f);
            E = new Spell(SpellSlot.E, 475f);
            R = new Spell(SpellSlot.R, 3000f);

            Q.SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(1f, 160f, 2000f, false, SkillshotType.SkillshotLine);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            Config = new Menu("Ezreal - the Dream Chaser", "Ezreal", true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q in Combo").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W in Combo").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E in Combo").SetValue(new StringList(new[] { "To mouse", "To enemy", "No" })));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q in Harass").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W in Harass").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("harassMana", "Min. Mana Percent").SetValue(new Slider(50, 100, 0)));

            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLane", "Clear with Q").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("laneMana", "Min. Mana Percent").SetValue(new Slider(50, 100, 0)));

            Config.AddSubMenu(new Menu("JungleClear", "JungleClear"));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("UseQJungle", "Clear with Q").SetValue(true));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("jungleMana", "Min. Mana Percent").SetValue(new Slider(50, 100, 0)));

            Config.AddSubMenu(new Menu("Killsteal", "Killsteal"));
            Config.SubMenu("Killsteal").AddItem(new MenuItem("UseQKillsteal", "Killsteal with Q").SetValue(true));
            Config.SubMenu("Killsteal").AddItem(new MenuItem("UseWKillsteal", "Killsteal with W").SetValue(true));
            Config.SubMenu("Killsteal").AddItem(new MenuItem("UseRKillsteal", "Killsteal with R").SetValue(true));

            Config.AddSubMenu(new Menu("Drawing", "Drawing"));
            Config.SubMenu("Drawing").AddItem(new MenuItem("qDraw", "Draw Q Range").SetValue(true));
            Config.SubMenu("Drawing").AddItem(new MenuItem("wDraw", "Draw W Range").SetValue(true));
            Config.SubMenu("Drawing").AddItem(new MenuItem("eDraw", "Draw E Range").SetValue(true));

            Config.AddToMainMenu();

            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var drawQ = Config.Item("qDraw").GetValue<bool>();
            if (drawQ && Q.IsReady())
                Render.Circle.DrawCircle(Player.Position, Q.Range, Colors.MistyRose);

            var drawW = Config.Item("wDraw").GetValue<bool>();
            if (drawW && W.IsReady())
                Render.Circle.DrawCircle(Player.Position, W.Range, Colors.MistyRose);

            var drawE = Config.Item("eDraw").GetValue<bool>();
            if (drawE && E.IsReady())
                Render.Circle.DrawCircle(Player.Position, E.Range, Colors.MistyRose);
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    LaneClear();
                    JungleClear();
                    break;
            }

            Killsteal();
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (target == null) return;

            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();

            switch (Config.Item("UseECombo").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                    if (E.IsReady() && Player.Distance(target) <= Q.Range + E.Range)
                        E.Cast(Game.CursorPos);
                    break;
                case 1:
                    if (E.IsReady() && Player.Distance(target) <= Q.Range)
                        E.Cast(target.ServerPosition);
                    break;
                case 2:
                    return;
            }

            if (useQ && Q.IsReady())
                Q.CastIfHitchanceEquals(target, HitChance.High);
            if (useW && W.IsReady())
                W.CastIfHitchanceEquals(target, HitChance.High);
        }

        private static void Harass()
        {
            if (Config.Item("harassMana").GetValue<Slider>().Value >= (Player.Mana / Player.MaxMana) * 100) return;

            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (target == null) return;

            var useQ = Config.Item("UseQHarass").GetValue<bool>();
            var useW = Config.Item("UseWHarass").GetValue<bool>();

            if (useQ && Q.IsReady())
                Q.CastIfHitchanceEquals(target, HitChance.High);
            if (useW && W.IsReady())
                W.CastIfHitchanceEquals(target, HitChance.High);
        }

        private static void JungleClear()
        {
            if (Config.Item("jungleMana").GetValue<Slider>().Value >= (Player.Mana / Player.MaxMana) * 100) return;

            var useQ = Config.Item("UseQJungle").GetValue<bool>();
            var mobs = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (mobs.Count <= 0) return;

            var mob = mobs[0];

            if (useQ && Q.IsReady())
                Q.CastIfHitchanceEquals(mob, HitChance.High);
        }

        private static void LaneClear()
        {
            if (Config.Item("laneMana").GetValue<Slider>().Value >= (Player.Mana / Player.MaxMana) * 100) return;

            var useQ = Config.Item("UseQLane").GetValue<bool>();
            var minions = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly);

            if (minions.Count <= 0) return;

            var minion = minions[0];

            if (useQ && Q.IsReady())
                Q.CastIfHitchanceEquals(minion, HitChance.High);
        }

        private static void Killsteal()
        {
            var useQ = Config.Item("UseQKillsteal").GetValue<bool>();
            var useW = Config.Item("UseWKillsteal").GetValue<bool>();
            var useR = Config.Item("UseRKillsteal").GetValue<bool>();

            foreach (
                var target in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(target => !target.IsMe && target.Team != Player.Team))
            {
                if (useQ && Q.IsReady() && Q.GetDamage(target) > target.Health
                    && Player.Distance(target) <= Q.Range)
                    Q.CastIfHitchanceEquals(target, HitChance.High);

                if (useW && W.IsReady() && W.GetDamage(target) > target.Health
                    && Player.Distance(target) <= W.Range)
                    W.CastIfHitchanceEquals(target, HitChance.High);

                if (useR && R.IsReady() && R.GetDamage(target) > target.Health
                    && Player.Distance(target) <= R.Range && Player.Distance(target) >= Orbwalking.GetRealAutoAttackRange(Player))
                    R.Cast(target);
            }
        }
    }
}