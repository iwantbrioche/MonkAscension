using BepInEx;
using RWCustom;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MonkAscension
{
    public partial class MonkAscension : BaseUnityPlugin
    {
        private void OnEnable()
        {
            On.RainWorld.OnModsInit += RainWorldOnOnModsInit;
        }

        private bool IsInit;

        private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            try
            {
                if (IsInit) return;

                On.Player.ctor += Player_ctor;
                On.Player.ClassMechanicsSaint += Player_ClassMechanicsSaint;

                On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
                On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;

                IsInit = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        public static Dictionary<PlayerGraphics, int> godPipsIndex = new();
        // Dictionary for keeping track of the indexes of the added sprites

        private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (self.SlugCatClass == SlugcatStats.Name.Yellow)
            {
                // Sets the maxGodTime for the godTimer
                self.maxGodTime = (int)(200f + 40f * (float)self.Karma);
                // You won't need this if statement if your slugcat doesn't go to Rubicon
                if (self.room != null && self.room.world.name == "HR")
                {
                    self.maxGodTime = 560f;
                }

                self.godTimer = self.maxGodTime;
            }
        }

        private void Player_ClassMechanicsSaint(On.Player.orig_ClassMechanicsSaint orig, Player self)
        {
            orig(self);
            if (self.SlugCatClass == SlugcatStats.Name.Yellow)
            {
                // Activate and Deactivate Ascension
                if (self.wantToJump > 0 && self.monkAscension)
                {
                    self.DeactivateAscension();
                    self.wantToJump = 0;
                }
                else if (self.wantToJump > 0 && self.input[0].pckp && self.canJump <= 0 && !self.monkAscension && self.bodyMode != Player.BodyModeIndex.Crawl && self.bodyMode != Player.BodyModeIndex.CorridorClimb && self.bodyMode != Player.BodyModeIndex.ClimbIntoShortCut && self.animation != Player.AnimationIndex.HangFromBeam && self.animation != Player.AnimationIndex.ClimbOnBeam && self.bodyMode != Player.BodyModeIndex.WallClimb && self.bodyMode != Player.BodyModeIndex.Swimming && self.Consious && !self.Stunned && self.godTimer > 0f && self.animation != Player.AnimationIndex.AntlerClimb && self.animation != Player.AnimationIndex.VineGrab && self.animation != Player.AnimationIndex.ZeroGPoleGrab)
                {
                    self.ActivateAscension();
                }

            }
        }

        private void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            if (self.player.SlugCatClass == SlugcatStats.Name.Yellow)
            {
                Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + self.numGodPips + 2);
                // Resizes the sLeaser sprite array to add the amount of numGodPips (12), and 2 for the crosshair and energy burst
                // If you have already resized the sprite array then you need to combine it with above, you will also need to adjust the dictionary as well

                if (godPipsIndex.ContainsKey(self)) { godPipsIndex[self] = sLeaser.sprites.Length - self.numGodPips - 2; }
                else { godPipsIndex.Add(self, sLeaser.sprites.Length - self.numGodPips - 2); }
                // Add self as a key for godPipsIndex dictionary and store the first index of numGodPips + 2
                // godPipsIndex[self] = 13 (index of energy burst)
                // godPipsIndex[self] + 1 = 14 (index of crosshair)
                // godPipsIndex[self] + 2 = 15 (starting index of numGodPips)

                sLeaser.sprites[godPipsIndex[self]] = new FSprite("Futile_White");
                sLeaser.sprites[godPipsIndex[self]].shader = rCam.game.rainWorld.Shaders["FlatLight"];
                // Set sprite for energy burst on ascension

                rCam.ReturnFContainer("Midground").AddChild(sLeaser.sprites[godPipsIndex[self]]);
                // Add to FContainer Midground

                sLeaser.sprites[godPipsIndex[self] + 1] = new FSprite("guardEye");
                // Set sprite for ascension crosshair

                for (int i = 0; i < self.numGodPips; i++)
                {
                    sLeaser.sprites[godPipsIndex[self] + 2 + i] = new FSprite("WormEye");
                    // Set sprite for the godPips timer

                    sLeaser.sprites[godPipsIndex[self] + 2 + i].RemoveFromContainer();
                    rCam.ReturnFContainer("HUD2").AddChild(sLeaser.sprites[godPipsIndex[self] + 2 + i]);
                    // Remove from container and add godPips to FContainer HUD2
                }
            }
        }
        private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (self.player.room != null && self.player.SlugCatClass == SlugcatStats.Name.Yellow)
            {
                // Taken from saint's code, handles ascension crosshair, godPips, and effects
                if (self.player.killFac > 0f || self.player.forceBurst)
                {
                    sLeaser.sprites[godPipsIndex[self]].isVisible = true;
                    sLeaser.sprites[godPipsIndex[self]].x = sLeaser.sprites[3].x + self.player.burstX;
                    sLeaser.sprites[godPipsIndex[self]].y = sLeaser.sprites[3].y + self.player.burstY + 60f;
                    float f = Mathf.Lerp(self.player.lastKillFac, self.player.killFac, timeStacker);
                    sLeaser.sprites[godPipsIndex[self]].scale = Mathf.Lerp(50f, 2f, Mathf.Pow(f, 0.5f));
                    sLeaser.sprites[godPipsIndex[self]].alpha = Mathf.Pow(f, 3f);
                }
                else
                {
                    sLeaser.sprites[godPipsIndex[self]].isVisible = false;
                }

                if (self.player.killWait > self.player.lastKillWait || self.player.killWait == 1f || self.player.forceBurst)
                {
                    self.rubberMouseX += (self.player.burstX - self.rubberMouseX) * 0.3f;
                    self.rubberMouseY += (self.player.burstY - self.rubberMouseY) * 0.3f;
                }
                else
                {
                    self.rubberMouseX *= 0.15f;
                    self.rubberMouseY *= 0.25f;
                }

                if (Mathf.Sqrt(Mathf.Pow(sLeaser.sprites[3].x - self.rubberMarkX, 2f) + Mathf.Pow(sLeaser.sprites[3].y - self.rubberMarkY, 2f)) > 100f)
                {
                    self.rubberMarkX = sLeaser.sprites[3].x;
                    self.rubberMarkY = sLeaser.sprites[3].y;
                }
                else
                {
                    self.rubberMarkX += (sLeaser.sprites[3].x - self.rubberMarkX) * 0.15f;
                    self.rubberMarkY += (sLeaser.sprites[3].y - self.rubberMarkY) * 0.25f;
                }

                sLeaser.sprites[godPipsIndex[self] + 1].x = self.rubberMarkX;
                sLeaser.sprites[godPipsIndex[self] + 1].y = self.rubberMarkY + 60f;
                float num16;
                if (self.player.monkAscension)
                {
                    sLeaser.sprites[9].color = Custom.HSL2RGB(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
                    sLeaser.sprites[10].alpha = 0f;
                    sLeaser.sprites[11].alpha = 0f;
                    sLeaser.sprites[godPipsIndex[self] + 1].color = sLeaser.sprites[9].color;
                    num16 = 1f;
                }
                else
                {
                    num16 = 0f;
                }

                float num17;
                if ((self.player.godTimer < self.player.maxGodTime || self.player.monkAscension) && !self.player.hideGodPips)
                {
                    num17 = 1f;
                    float num18 = 15f;
                    if (!self.player.monkAscension)
                    {
                        num18 = 6f;
                    }

                    self.rubberRadius += (num18 - self.rubberRadius) * 0.045f;
                    if (self.rubberRadius < 5f)
                    {
                        self.rubberRadius = num18;
                    }

                    float num19 = self.player.maxGodTime / (float)self.numGodPips;
                    for (int m = 0; m < self.numGodPips; m++)
                    {
                        float num20 = num19 * (float)m;
                        float num21 = num19 * (float)(m + 1);
                        if (self.player.godTimer <= num20)
                        {
                            sLeaser.sprites[godPipsIndex[self] + 2 + m].scale = 0f;
                        }
                        else if (self.player.godTimer >= num21)
                        {
                            sLeaser.sprites[godPipsIndex[self] + 2 + m].scale = 1f;
                        }
                        else
                        {
                            sLeaser.sprites[godPipsIndex[self] + 2 + m].scale = (self.player.godTimer - num20) / num19;
                        }

                        if (self.player.karmaCharging > 0 && self.player.monkAscension)
                        {
                            sLeaser.sprites[godPipsIndex[self] + 2 + m].color = sLeaser.sprites[9].color;
                        }
                        else
                        {
                            sLeaser.sprites[godPipsIndex[self] + 2 + m].color = PlayerGraphics.SlugcatColor(self.CharacterForColor);
                        }
                    }
                }
                else
                {
                    num17 = 0f;
                }

                sLeaser.sprites[godPipsIndex[self] + 1].x = self.rubberMarkX + self.rubberMouseX;
                sLeaser.sprites[godPipsIndex[self] + 1].y = self.rubberMarkY + 60f + self.rubberMouseY;
                self.rubberAlphaEmblem += (num16 - self.rubberAlphaEmblem) * 0.05f;
                self.rubberAlphaPips += (num17 - self.rubberAlphaPips) * 0.05f;
                sLeaser.sprites[godPipsIndex[self] + 1].alpha = self.rubberAlphaEmblem;
                sLeaser.sprites[10].alpha *= 1f - self.rubberAlphaPips;
                sLeaser.sprites[11].alpha *= 1f - self.rubberAlphaPips;
                for (int n = godPipsIndex[self] + 2; n < godPipsIndex[self] + 2 + self.numGodPips; n++)
                {
                    sLeaser.sprites[n].alpha = self.rubberAlphaPips;
                    Vector2 vector16 = new Vector2(sLeaser.sprites[14].x, sLeaser.sprites[14].y);
                    vector16 += Custom.rotateVectorDeg(Vector2.one * self.rubberRadius, (float)(n - 15) * (360f / (float)self.numGodPips));
                    sLeaser.sprites[n].x = vector16.x;
                    sLeaser.sprites[n].y = vector16.y;
                }
            }
        }
    }
}
