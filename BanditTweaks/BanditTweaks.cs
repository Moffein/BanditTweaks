using BepInEx;
using System;
using RoR2;
using UnityEngine;
using EntityStates;
using BepInEx.Configuration;
using EntityStates.Bandit2;
using System.Runtime.CompilerServices;
using R2API.Utils;
using MonoMod.RuntimeDetour;

namespace BanditTweaks
{
    [BepInDependency("com.RiskyLives.RiskyMod", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("de.userstorm.banditweaponmodes", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.Moffein.BanditTweaks", "Bandit Tweaks", "1.5.2")]
    public class BanditTweaks : BaseUnityPlugin
    {
        public enum BanditFireMode
        {
            Tap, Spam
        }

        bool enableFireSelect = true;
        BanditFireMode fireMode = BanditFireMode.Tap;
        bool selectWithScrollWheel = true;
        KeyCode selectButton = KeyCode.None;
        KeyCode defaultButton = KeyCode.None;
        KeyCode burstButton = KeyCode.None;
        bool slayerFix = true;

        public static bool quickdrawEnabled = false;
        public static bool RiskyModLoaded = false;

        bool backstabBandFix = true;

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private bool CheckRiskyModBandit2Core()
        {
            return !RiskyMod.Survivors.Bandit2.Bandit2Core.enabled;
        }

        public void Awake()
        {
            RiskyModLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.RiskyLives.RiskyMod");
            bool shouldRun = true;
            if (RiskyModLoaded)
            {
                shouldRun = CheckRiskyModBandit2Core();
            }
            if (!shouldRun)
            {
                Debug.LogError("BanditTweaks: RiskyMod's Bandit changes are enabled, aborting Awake(). BanditTweaks and RiskyMod's Bandit changes are highly incompatible, so you should disable this mod or RiskyMod's Bandit tweaks in the config.");
                return;
            }

            float backstabCritBonus = base.Config.Bind<float>(new ConfigDefinition("00 - Passive", "Backstab Crit Bonus"), 1.5f, new ConfigDescription("*SERVER-SIDE* Multiply backstab damage if the attack is already a crit. Set to 1 to disable.")).Value;
            if (backstabCritBonus < 1f)
            {
                backstabCritBonus = 1f;
            }

            backstabBandFix = base.Config.Bind<bool>(new ConfigDefinition("00 - Passive", "Backstab Crit Elemental Band Fix"), true, new ConfigDescription("*SERVER-SIDE* Prevents crit backstabs from making weak attacks able to trigger Bands by making weak attacks hit an extra time for 50% damage (0.5 proc) instead.")).Value;

            quickdrawEnabled = base.Config.Bind<bool>(new ConfigDefinition("00 - Passive", "Enable Quickdraw"), false, new ConfigDescription("Using other skills will instantly reload your Primary.")).Value;
            
            
            bool enableAutoFire = base.Config.Bind<bool>(new ConfigDefinition("01 - Primary", "Enable Autofire"), true, new ConfigDescription("Holding down the Primary button automatically fires your gun.")).Value;
            enableFireSelect = base.Config.Bind<bool>(new ConfigDefinition("01 - Primary", "Enable Firemode Selection"), true, new ConfigDescription("Enables swapping primary firemode between slow and fast fire.")).Value;
            selectWithScrollWheel = base.Config.Bind<bool>(new ConfigDefinition("01 - Primary", "Select with ScrollWheel"), true, new ConfigDescription("Scroll wheel swaps between firemodes.")).Value;
            selectButton = base.Config.Bind<KeyCode>(new ConfigDefinition("01 - Primary", "Select Button"), KeyCode.None, new ConfigDescription("Button to swap between firemodes.")).Value;
           
            defaultButton = base.Config.Bind<KeyCode>(new ConfigDefinition("01 - Primary", "Tapfire Button"), KeyCode.None, new ConfigDescription("Button to swap to Default firemode.")).Value;
            burstButton = base.Config.Bind<KeyCode>(new ConfigDefinition("01 - Primary", "Spamfire Button"), KeyCode.None, new ConfigDescription("Button to swap to Burst firemode.")).Value;
            float autoFireDuration = base.Config.Bind<float>(new ConfigDefinition("01 - Primary", "Tap Fire Rate"), 0.3f, new ConfigDescription("How long it takes to autofire shots on the Default firemode.")).Value;
            float burstFireDuration = base.Config.Bind<float>(new ConfigDefinition("01 - Primary", "Spam Fire Rate"), 0.12f, new ConfigDescription("How long it takes to autofire shots on the Burst firemode.")).Value;
            bool prioritizeReload = base.Config.Bind<bool>(new ConfigDefinition("01 - Primary", "Prioritize Reload"), false, new ConfigDescription("Makes reloading take priority over shooting.")).Value;

            float burstBulletRadius = base.Config.Bind<float>(new ConfigDefinition("01a - Burst", "Bullet Radius"), 0.3f, new ConfigDescription("How wide bullets are (0 is vanilla).")).Value;
            float blastBulletRadius = base.Config.Bind<float>(new ConfigDefinition("01b - Blast", "Bullet Radius"), 0.4f, new ConfigDescription("How wide bullets are (0 is vanilla).")).Value;

            bool knifeTweaks = base.Config.Bind<bool>(new ConfigDefinition("02 - Secondary", "Serrated Dagger Tweaks"), true, new ConfigDescription("Serrated Dagger lunges while sprinting and has a larger hitbox.")).Value;
            bool noKnifeAttackSpeed = base.Config.Bind<bool>(new ConfigDefinition("02 - Secondary", "Serrated Dagger Minimum Duration"), true, new ConfigDescription("Serrated Dagger has a minimum duration of 0.3s so that the lunge doesn't stop working at high attack speeds.")).Value;

            bool cloakAnim = base.Config.Bind<bool>(new ConfigDefinition("03 - Utility", "Smokebomb Anim while grounded"), true, new ConfigDescription("Enable the Smokebomb animation when on the ground.")).Value;

            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("de.userstorm.banditweaponmodes"))
            {
                enableAutoFire = false;
            }
            
            if (!enableAutoFire)
            {
                enableFireSelect = false;
            }

            if (knifeTweaks)
            {
                //Increase Hitbox Size
                CharacterBody cb = RoR2Content.Survivors.Bandit2.bodyPrefab.GetComponent<CharacterBody>();
                HitBoxGroup hbg = cb.GetComponentInChildren<HitBoxGroup>();
                if (hbg.groupName == "SlashBlade")
                {
                    Transform hitboxTransform = hbg.hitBoxes[0].transform;
                    hitboxTransform.localScale = new Vector3(hitboxTransform.localScale.x, hitboxTransform.localScale.y * 1.4f, hitboxTransform.localScale.z * 1.3f);
                    hitboxTransform.localPosition += new Vector3(0f, 0f, 1f);
                }

                Keyframe kf1 = new Keyframe(0f, 3f, -8.182907104492188f, -3.3333332538604738f, 0f, 0.058712735772132876f);
                kf1.weightedMode = WeightedMode.None;
                kf1.tangentMode = 65;

                Keyframe kf2 = new Keyframe(0.3f, 0f, -3.3333332538604738f, -3.3333332538604738f, 0.3333333432674408f, 0.3333333432674408f);    //Time should match up with SlashBlade min duration (hitbox length)
                kf2.weightedMode = WeightedMode.None;
                kf2.tangentMode = 34;

                Keyframe[] keyframes = new Keyframe[2];
                keyframes[0] = kf1;
                keyframes[1] = kf2;

                AnimationCurve knifeVelocity = new AnimationCurve
                {
                    preWrapMode = WrapMode.ClampForever,
                    postWrapMode = WrapMode.ClampForever,
                    keys = keyframes
                };

                On.EntityStates.Bandit2.Weapon.SlashBlade.OnEnter += (orig, self) =>
                {
                    if (noKnifeAttackSpeed)
                    {
                        self.ignoreAttackSpeed = true;
                    }
                    orig(self);
                    if (self.characterBody && self.characterBody.isSprinting)
                    {
                        self.forceForwardVelocity = true;
                        self.forwardVelocityCurve = knifeVelocity;
                    }
                };

                if (noKnifeAttackSpeed)
                {
                    var getBandit2SlashBladeMinDuration = new Hook(typeof(EntityStates.Bandit2.Weapon.SlashBlade).GetMethodCached("get_minimumDuration"),
                    typeof(BanditTweaks).GetMethodCached(nameof(GetBandit2SlashBladeMinDurationHook)));
                }
            }

            bool cloakRequireRepress = !base.Config.Bind<bool>(new ConfigDefinition("03 - Utility", "Hold to Cloak"), true, new ConfigDescription("Holding down the Utility button cloaks you as soon as it is off cooldown.")).Value;
            float minCloakDuration = base.Config.Bind<float>(new ConfigDefinition("03 - Utility", "Minimum Cloak Duration"), 0.3f, new ConfigDescription("Minimum duration before other skills can be used after cloaking if Cloak Requires re-press is disabled.")).Value;
            if (minCloakDuration <= 0f)
            {
                cloakRequireRepress = true;
            }

            slayerFix = base.Config.Bind<bool>(new ConfigDefinition("04 - Special", "Slayer Fix"), true, new ConfigDescription("*SERVER-SIDE* Slayer (bonus damage against low HP enemies) now affects procs.")).Value;
            bool specialHold = base.Config.Bind<bool>(new ConfigDefinition("04 - Special", "Hold to Aim"), true, new ConfigDescription("The Special button can be held down to aim your shot. The shot will only shoot once you release.")).Value;
            bool specialSprintCancel = base.Config.Bind<bool>(new ConfigDefinition("04 - Special", "Cancel by Sprinting"), false, new ConfigDescription("Sprinting cancels your special.")).Value;
            float graceDuration = base.Config.Bind<float>(new ConfigDefinition("04 - Special", "Grace Period Duration"), 1f, new ConfigDescription("*SERVER-SIDE* Triggers Special on-kill effect if enemy dies within this time window. 0 disables.")).Value;
            float executeThreshold = base.Config.Bind<float>(new ConfigDefinition("04 - Special", "Execute Threshold"), 0f, new ConfigDescription("*SERVER-SIDE* Bandit's Specials instanatly kill enemies below this HP percent. 0 = disabled, 1.0 = 100% HP.")).Value;
            

            if (RiskyModLoaded)
            {
                slayerFix = false;
                graceDuration = 0f;
            }

            GracePeriodComponent.graceDuration = graceDuration;
            GameObject BanditObject = Resources.Load<GameObject>("prefabs/characterbodies/bandit2body");
            SkillLocator skills = BanditObject.GetComponent<SkillLocator>();

            skills.utility.skillFamily.variants[0].skillDef.mustKeyPress = cloakRequireRepress;

            skills.special.skillFamily.variants[0].skillDef.canceledFromSprinting = specialSprintCancel;
            skills.special.skillFamily.variants[1].skillDef.canceledFromSprinting = specialSprintCancel;

            if (burstBulletRadius > 0f)
            {
                On.EntityStates.Bandit2.Weapon.FireShotgun2.ModifyBullet += (orig, self, bulletAttack) =>
                {
                    bulletAttack.radius = burstBulletRadius;
                    orig(self, bulletAttack);
                };
            }

            if (blastBulletRadius > 0f)
            {
                On.EntityStates.Bandit2.Weapon.Bandit2FireRifle.ModifyBullet += (orig, self, bulletAttack) =>
                {
                    bulletAttack.radius = blastBulletRadius;
                    orig(self, bulletAttack);
                };
            }

            if (enableAutoFire)
            {
                skills.primary.skillFamily.variants[0].skillDef.mustKeyPress = false;
                skills.primary.skillFamily.variants[0].skillDef.interruptPriority = prioritizeReload ? InterruptPriority.Any : InterruptPriority.Skill;

                skills.primary.skillFamily.variants[1].skillDef.mustKeyPress = false;
                skills.primary.skillFamily.variants[1].skillDef.interruptPriority = prioritizeReload ? InterruptPriority.Any : InterruptPriority.Skill;

                On.EntityStates.Bandit2.Weapon.Bandit2FirePrimaryBase.GetMinimumInterruptPriority += (orig, self) =>
                {
                    if (self.fixedAge <= self.minimumDuration && self.inputBank.skill1.wasDown)
                    {
                        return InterruptPriority.PrioritySkill;
                    }
                    return InterruptPriority.Any;
                };

                On.EntityStates.Bandit2.Weapon.Bandit2FirePrimaryBase.OnEnter += (orig, self) =>
                {
                    if (fireMode == BanditFireMode.Tap)
                    {
                        self.minimumBaseDuration = autoFireDuration;
                    }
                    else
                    {
                        self.minimumBaseDuration = burstFireDuration;
                    }
                    orig(self);
                };
            }

            BanditObject.AddComponent<QuickdrawComponent>();

            if (!cloakRequireRepress)
            {
                On.EntityStates.Bandit2.StealthMode.GetMinimumInterruptPriority += (orig, self) =>
                {
                    return self.fixedAge > minCloakDuration ? InterruptPriority.Skill : InterruptPriority.Pain;
                };
            }

            On.EntityStates.Bandit2.StealthMode.OnExit += (orig, self) =>
            {
                orig(self);

                Util.PlaySound(EntityStates.Bandit2.StealthMode.exitStealthSound, self.gameObject);
            };

            On.EntityStates.Bandit2.StealthMode.FireSmokebomb += (orig, self) =>
            {
                if (self.isAuthority)
                {
                    BlastAttack blastAttack = new BlastAttack();
                    blastAttack.radius = StealthMode.blastAttackRadius;
                    blastAttack.procCoefficient = StealthMode.blastAttackProcCoefficient;
                    blastAttack.position = self.transform.position;
                    blastAttack.attacker = self.gameObject;
                    blastAttack.crit = self.characterBody.RollCrit();
                    blastAttack.baseDamage = self.damageStat * StealthMode.blastAttackDamageCoefficient;
                    blastAttack.falloffModel = BlastAttack.FalloffModel.None;
                    blastAttack.damageType = DamageType.Stun1s;
                    blastAttack.baseForce = StealthMode.blastAttackForce;
                    blastAttack.teamIndex = TeamComponent.GetObjectTeam(blastAttack.attacker);
                    blastAttack.attackerFiltering = AttackerFiltering.NeverHit;
                    blastAttack.Fire();
                }
                if (StealthMode.smokeBombEffectPrefab)
                {
                    EffectManager.SimpleMuzzleFlash(StealthMode.smokeBombEffectPrefab, self.gameObject, StealthMode.smokeBombMuzzleString, false);
                }
                if (self.characterMotor)
                {
                    self.characterMotor.velocity = new Vector3(self.characterMotor.velocity.x, StealthMode.shortHopVelocity, self.characterMotor.velocity.z);
                }
            };

            On.EntityStates.Bandit2.ThrowSmokebomb.OnEnter += (orig, self) =>
            {
                if (!cloakAnim && self.characterBody && self.characterMotor && self.characterMotor.isGrounded)
                {
                    self.attackSpeedStat = self.characterBody.attackSpeed;
                    self.damageStat = self.characterBody.damage;
                    self.critStat = self.characterBody.crit;
                    self.moveSpeedStat = self.characterBody.moveSpeed;
                }
                else
                {
                    orig(self);
                }
            };

            On.EntityStates.Bandit2.ThrowSmokebomb.GetMinimumInterruptPriority += (orig, self) =>
            {
                if (!cloakRequireRepress)
                {
                    return self.fixedAge > minCloakDuration ? InterruptPriority.PrioritySkill : InterruptPriority.Pain;
                }
                else
                {
                    return orig(self);
                }
            };

            //This fixes sprint cancel that is hardcoded into Bandit's Specials
            On.EntityStates.Bandit2.Weapon.BaseSidearmState.FixedUpdate += (orig, self) =>
            {
                if (!specialSprintCancel)
                {
                    self.fixedAge += Time.fixedDeltaTime;
                }
                else
                {
                    orig(self);
                }
            };

            On.EntityStates.Bandit2.Weapon.BasePrepSidearmRevolverState.FixedUpdate += (orig, self) =>
            {
                if (specialHold)
                {
                    self.fixedAge += Time.fixedDeltaTime;
                    if (self.fixedAge > self.duration && !self.inputBank.skill4.down)
                    {
                        self.outer.SetNextState(self.GetNextState());
                    }
                }
                else
                {
                    orig(self);
                }
            };

            On.RoR2.HealthComponent.TakeDamage += (orig, self, damageInfo) =>
            {
                if (slayerFix && (damageInfo.damageType & DamageType.BonusToLowHealth) > DamageType.Generic)
                {
                    damageInfo.damageType &= ~DamageType.BonusToLowHealth;
                    damageInfo.damage *= Mathf.Lerp(3f, 1f, self.combinedHealthFraction);
                }

                bool backstabSecondHit = false;
                DamageInfo backstabSecondDamageInfo = null;

                DamageType dt = damageInfo.damageType & (DamageType.ResetCooldownsOnKill | DamageType.GiveSkullOnKill);
                GracePeriodComponent gc = null;
                CharacterBody attackerBody = null;
                if (damageInfo.attacker)
                {
                    attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                    if (attackerBody && attackerBody.baseNameToken == "BANDITRELOADED_BODY_NAME")
                    {
                        attackerBody = null;
                    }
                }
                if (attackerBody)
                {
                    if (graceDuration > 0f)
                    {
                        gc = self.gameObject.GetComponent<GracePeriodComponent>();
                        if (!gc)
                        {
                            gc = self.gameObject.AddComponent<GracePeriodComponent>();
                        }

                        if (self.alive && (damageInfo.damageType & DamageType.ResetCooldownsOnKill) == 0 && gc.HasReset(attackerBody))
                        {
                            damageInfo.damageType |= DamageType.ResetCooldownsOnKill;
                        }
                        if (self.alive && (damageInfo.damageType & DamageType.GiveSkullOnKill) == 0 && gc.HasSkull(attackerBody))
                        {
                            damageInfo.damageType |= DamageType.GiveSkullOnKill;
                        }
                    }

                    if (backstabCritBonus > 1f && damageInfo.damage > 0f && damageInfo.crit)
                    {
                        Vector3 vector = attackerBody.corePosition - damageInfo.position;
                        if ((attackerBody.canPerformBackstab && (damageInfo.damageType & DamageType.DoT) != DamageType.DoT && (!damageInfo.procChainMask.HasProc(ProcType.Backstab) )
                        && BackstabManager.IsBackstab(-vector, self.body)))
                        {
                            float damageCoefficient = damageInfo.damage / attackerBody.damage;
                            if (!backstabBandFix || damageCoefficient >= 4f)
                            {
                                damageInfo.damage *= backstabCritBonus;
                            }
                            else
                            {
                                backstabSecondHit = true;
                                backstabSecondDamageInfo = new DamageInfo
                                {
                                    attacker = damageInfo.attacker,
                                    crit = damageInfo.crit,
                                    damage = damageInfo.damage * (backstabCritBonus - 1f),
                                    damageColorIndex = damageInfo.damageColorIndex,
                                    damageType = damageInfo.damageType & ~DamageType.SuperBleedOnCrit,
                                    dotIndex = damageInfo.dotIndex,
                                    force = damageInfo.force * 0.5f,
                                    inflictor = damageInfo.inflictor,
                                    position = damageInfo.position,
                                    procChainMask = damageInfo.procChainMask,
                                    procCoefficient = damageInfo.procCoefficient * 0.5f,
                                    rejected = damageInfo.rejected
                                };
                                backstabSecondDamageInfo.procChainMask.AddProc(ProcType.Backstab);
                            }
                        }
                    }
                }

                orig(self, damageInfo);

                if (graceDuration > 0f && gc && attackerBody && attackerBody.master && !damageInfo.rejected)
                {
                    if (self.alive)
                    {
                        gc.AddTimer(attackerBody, dt);
                    }
                    else
                    {
                        gc.TriggerEffects(attackerBody);
                    }
                }

                if (self.alive)
                {
                    if (backstabSecondHit && !damageInfo.rejected)
                    {
                        orig(self, backstabSecondDamageInfo);
                    }

                    if (executeThreshold > 0f && (damageInfo.damageType & (DamageType.ResetCooldownsOnKill | DamageType.GiveSkullOnKill)) > 0 && attackerBody)
                    {
                        float executePercent = self.isInFrozenState ? 0.3f : 0f;
                        if (attackerBody.executeEliteHealthFraction > executePercent)
                        {
                            executePercent = attackerBody.executeEliteHealthFraction;
                        }
                        executePercent += executeThreshold;
                        if (executeThreshold > self.combinedHealthFraction)
                        {
                            damageInfo.damage = self.combinedHealth / 2f + 1f;
                            damageInfo.damageType |= DamageType.BypassArmor;
                            damageInfo.procCoefficient = 0f;
                            damageInfo.crit = true;
                            damageInfo.damageColorIndex = DamageColorIndex.WeakPoint;
                            orig(self, damageInfo);
                        }
                    }
                }
            };

            On.RoR2.UI.SkillIcon.Update += (orig, self) =>
            {
                orig(self);
                if (enableFireSelect && self.targetSkill && self.targetSkillSlot == SkillSlot.Primary)
                {
                    if (self.targetSkill.characterBody.baseNameToken == "BANDIT2_BODY_NAME")
                    {
                        self.stockText.gameObject.SetActive(true);
                        self.stockText.fontSize = 12f;
                        self.stockText.SetText(fireMode.ToString() + "\n" + self.targetSkill.stock);
                    }
                }
            };
        }

        private static float GetBandit2SlashBladeMinDurationHook(EntityStates.Bandit2.Weapon.SlashBlade self)
        {
            return 0.3f;
        }

        public void ToggleFireMode()
        {
            if (fireMode == BanditFireMode.Tap)
            {
                fireMode = BanditFireMode.Spam;
                return;
            }
            fireMode = BanditFireMode.Tap;
        }
        public void Update()
        {
            if (!enableFireSelect)
            {
                return;
            }
            if (selectWithScrollWheel && Input.GetAxis("Mouse ScrollWheel") != 0)
            {
                ToggleFireMode();
            }
            if (Input.GetKeyDown(selectButton))
            {
                ToggleFireMode();
            }
            if (Input.GetKeyDown(defaultButton))
            {
                fireMode = BanditFireMode.Tap;
            }
            if (Input.GetKeyDown(burstButton))
            {
                fireMode = BanditFireMode.Spam;
            }
        }
    }
}

namespace R2API.Utils
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ManualNetworkRegistrationAttribute : Attribute
    {
    }
}

namespace EnigmaticThunder
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ManualNetworkRegistrationAttribute : Attribute
    {
    }
}
