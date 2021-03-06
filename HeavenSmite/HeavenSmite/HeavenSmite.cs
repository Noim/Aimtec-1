﻿
using System.Linq;

using Aimtec;
using Spell = Aimtec.SDK.Spell;
using Aimtec.SDK.Extensions;
using Aimtec.SDK.Menu;
using Aimtec.SDK.Menu.Components;
using System.Drawing;
using Aimtec.SDK.Util;
using Aimtec.SDK.Util.Cache;
using System;

namespace HeavenSmiteReborn
{
    internal class HeavenSmite
    {
        public static Menu Menu = new Menu("HeavenSmite", "HeavenSmite", true);
        private static Obj_AI_Hero Player => ObjectManager.GetLocalPlayer();
        private static int SmiteDamages
        {
            get
            {
                int[] Dmg = new int[] { 390, 410, 430, 450, 480, 510, 540, 570, 600, 640, 680, 720, 760, 800, 850, 900, 950, 1000 };

                return Dmg[Player.Level - 1];
            }
        }

        private static int SmiteDamagesChamp
        {
            get
            {
                int[] Dmg = new int[] { 28, 36, 44, 52, 60, 68, 76, 84, 92, 100, 108, 116, 124, 132, 140, 148, 156, 166 };

                return Dmg[Player.Level - 1];
            }
        }

        private static Spell Smite;
        private static string[] pMobs = new string[] { "SRU_Baron", "SRU_Blue", "SRU_Red", "SRU_RiftHerald" };
        private static string[] small = new string[] { "SRU_Murkwolf", "SRU_Razorbeak", "SRU_Gromp", "SRU_Krug", "Sru_Crab" };

        public HeavenSmite()
        {
            Smite = new Spell(Player.SpellBook.Spells.FirstOrDefault(spell => spell.Name.Contains("Smite")).Slot, 500);

            Menu.Add(new MenuKeyBind("Key", "Auto Smite:", Aimtec.SDK.Util.KeyCode.M, KeybindType.Toggle));

            var Dragons = new Menu("Dragons", "Dragons");
            {
                Dragons.Add(new MenuBool("SRU_Dragon_Air", "Air Dragon"));
                Dragons.Add(new MenuBool("SRU_Dragon_Fire", "Fire Dragon"));
                Dragons.Add(new MenuBool("SRU_Dragon_Earth", "Earth Dragon"));
                Dragons.Add(new MenuBool("SRU_Dragon_Water", "Water Dragon"));
                Dragons.Add(new MenuBool("SRU_Dragon_Elder", "Elder Dragon"));
            };
            Menu.Add(Dragons);
            var Big = new Menu("BigMobs", "Big Mobs");
            {
                Big.Add(new MenuBool("SRU_Baron", "Baron"));
                Big.Add(new MenuBool("SRU_Blue", "Blue"));
                Big.Add(new MenuBool("SRU_Red", "Red"));
                Big.Add(new MenuBool("SRU_RiftHerald", "Rift Herald"));
            }
            Menu.Add(Big);
            var Small = new Menu("SmallMobs", "Small Mobs");
            {
                Small.Add(new MenuBool("SRU_Gromp", "Gromp"));
                Small.Add(new MenuBool("SRU_Murkwolf", "Wolves"));
                Small.Add(new MenuBool("SRU_Krug", "Krug"));
                Small.Add(new MenuBool("SRU_Razorbeak", "Razor"));
                Small.Add(new MenuBool("Sru_Crab", "Crab"));
            }
            Menu.Add(Small);

            var Champion = new Menu("Champion", "Champions");
            {
                Champion.Add(new MenuBool("ChampionToggle", "Smite Champions", false));
                Champion.Add(new MenuBool("smiteCharge", "Always Hold 1 Smite Charge"));
                foreach (Obj_AI_Hero enemies in GameObjects.EnemyHeroes)
                    Champion.Add(new MenuBool("smiteKS" + enemies.ChampionName.ToLower(), enemies.ChampionName));
            }
            Menu.Add(Champion);

            var DrawMenu = new Menu("Draw", "Drawings");
            {
                DrawMenu.Add(new MenuBool("DrawSmiteRange", "Smite Range", false));
                DrawMenu.Add(new MenuBool("AutoSmiteToggle", "AutoSmite State"));
                DrawMenu.Add(new MenuBool("DrawDamage", "Draw Smite Damage"));
            }
            Menu.Add(DrawMenu);
            Menu.Attach();

            Render.OnPresent += Render_OnPresent;
            Game.OnUpdate += Game_OnUpdate;
        }

        private void Game_OnUpdate()
        {
            if (Player.IsDead && !Smite.Ready)
                return;

            if (Menu["Draw"]["DrawSmiteRange"].Enabled)
                Render.Circle(Player.Position, Smite.Range, 30, Color.LightGreen);

            if (Menu["Draw"]["AutoSmiteToggle"].Enabled)
            {
                if (Render.WorldToScreen(Player.Position, out Vector2 coord))
                {
                    coord.Y -= -30;
                    if (coord.X > 0 && coord.Y > 0 && coord.X < Render.Width && coord.Y < Render.Height)
                        Render.Text(coord.X, coord.Y, Menu["Key"].Enabled ? Color.LightGreen : Color.Red, Menu["Key"].Enabled ? "SMITE: ON" : "SMITE: OFF");
                }
            }

            if (!Menu["Key"].Enabled)
                return;

            foreach (var Obj in ObjectManager.Get<Obj_AI_Minion>().Where(x => x.IsValidTarget(Smite.Range) && SmiteDamages >= x.Health && !x.IsDead && x.IsValidSpellTarget()))
            {

                if (pMobs.Contains(Obj.UnitSkinName) || Obj.UnitSkinName.Contains("Baron"))
                {
                    if (Menu["BigMobs"][Obj.UnitSkinName].Enabled)
                        Smite.Cast(Obj);
                }

                if (Obj.UnitSkinName.Contains("Dragon"))
                {

                    if (Menu["Dragons"][Obj.UnitSkinName].Enabled)
                        Smite.Cast(Obj);
                }

                if (small.Contains(Obj.UnitSkinName))
                {
                    if (Menu["SmallMobs"][Obj.UnitSkinName].Enabled)
                        Smite.Cast(Obj);
                }
            }

            if (Menu["Champion"]["ChampionToggle"].Enabled)
            {
                foreach (var Obj in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(Smite.Range) && SmiteDamagesChamp >= x.Health && x.IsEnemy))
                {

                    if (Menu["Champion"]["smiteKS" + Obj.ChampionName.ToLower()].Enabled)
                    {
                        //Preserve smite charge?
                        if (Menu["Champion"]["smiteCharge"].Enabled)
                        {
                            if (Player.SpellBook.Spells.FirstOrDefault(spell => spell.Name.Contains("Smite")).Ammo <= 1)
                                return;
                        }

                        Smite.Cast(Obj);
                    }

                }

            }
        }

        private static void Render_OnPresent()
        {
            if (Menu["Draw"]["DrawDamage"].Enabled)
            {
                foreach (var minion in ObjectManager.Get<Obj_AI_Minion>().Where(x => x.IsValidTarget(1500) && !x.IsDead && x.IsValidSpellTarget() && x.IsVisible && x.IsFloatingHealthBarActive && x.IsValid))
                {
                    // Monster bar widths and offsets from ElSmite
                    var barWidth = 0;
                    var xOffset = 0;
                    var yOffset = 0;
                    var height = 10;
                    switch (minion.UnitSkinName)
                    {
                        case "SRU_Red":
                        case "SRU_Blue":
                        case "SRU_Dragon_Air":
                        case "SRU_Dragon_Earth":
                        case "SRU_Dragon_Fire":
                        case "SRU_Dragon_Water":
                        case "SRU_Dragon_Elder":
                        case "SRU_RiftHerald":
                            barWidth = 145;
                            xOffset = 10;
                            yOffset = 20;
                            height = 10;
                            break;

                        case "SRU_Baron":
                            barWidth = 194;
                            xOffset = 15;
                            yOffset = 23;
                            height = 13;
                            break;

                        case "Sru_Crab":
                            barWidth = 61;
                            xOffset = 0;
                            yOffset = 3;
                            height = 5;
                            break;

                        case "SRU_Krug":
                            barWidth = 92;
                            xOffset = 0;
                            yOffset = 5;
                            height = 5;
                            break;

                        case "SRU_Gromp":
                            barWidth = 92;
                            xOffset = 0;
                            yOffset = 5;
                            height = 5;
                            break;

                        case "SRU_Murkwolf":
                            barWidth = 92;
                            xOffset = 0;
                            yOffset = 5;
                            height = 5;
                            break;

                        case "SRU_Razorbeak":
                            barWidth = 75;
                            xOffset = 0;
                            yOffset = 5;
                            height = 5;
                            break;

                        default:
                            break;
                    }

                    var barPos = minion.FloatingHealthBarPosition;
                    var percentHealthAfterDamage = Math.Max(0, minion.Health - SmiteDamages) / minion.MaxHealth;
                    var xPosDamage = barPos.X + xOffset + barWidth * percentHealthAfterDamage;
                    var xPosCurrentHp = barPos.X + xOffset + barWidth * minion.Health / minion.MaxHealth;

                    Render.Rectangle(new Vector2((float)barPos.X + xOffset, barPos.Y + yOffset), (float)xPosCurrentHp - xPosDamage, height, Color.FromArgb(100, 255, 255, 0));

                }
            }
        }
            
    }
}