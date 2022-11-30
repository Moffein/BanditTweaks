using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BanditTweaks
{
    public class GracePeriodComponent : MonoBehaviour
    {
        public void FixedUpdate()
        {
            del.Clear();
            foreach (BanditTimer b in hitList)
            {
                if (b.skillLocator)
                {
                    b.length -= Time.fixedDeltaTime;
                    if (b.length <= 0f)
                    {
                        del.Add(b);
                    }
                }
            }
            foreach (BanditTimer b in del)
            {
                hitList.Remove(b);
            }
            del.Clear();
        }
        public void TriggerEffects(CharacterBody killerBody)
        {
            List<CharacterBody> resetList = new List<CharacterBody>();
            bool repeat;
            foreach (BanditTimer b in hitList)
            {
                repeat = false;
                foreach (CharacterBody s in resetList)
                {
                    if (s == b.body)
                    {
                        repeat = true;
                        break;
                    }
                }
                if (!repeat && b.body != killerBody)
                {
                    if (b.skillLocator && b.skillLocator.isActiveAndEnabled)
                    {
                        resetList.Add(b.body);
                        if ((b.damageType & DamageType.ResetCooldownsOnKill) > 0)
                        {
                            b.skillLocator.ResetSkills();
                        }
                        if ((b.damageType & DamageType.GiveSkullOnKill) > 0)
                        {
                            b.body.AddBuff(RoR2Content.Buffs.BanditSkull);
                        }
                    }
                }
            }
            hitList.Clear();
        }
        public void AddTimer(CharacterBody b, DamageType dt, float graceDuration)
        {
            if (b.skillLocator)
            {
                BanditTimer bt = new BanditTimer(b, b.skillLocator, graceDuration, dt);
                hitList.Add(bt);
            }
        }

        public bool HasSkull(CharacterBody body)
        {
            foreach (BanditTimer bt in hitList)
            {
                if (bt.body == body && (bt.damageType & DamageType.GiveSkullOnKill) > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasReset(CharacterBody body)
        {
            foreach (BanditTimer bt in hitList)
            {
                if (bt.body == body && (bt.damageType & DamageType.ResetCooldownsOnKill) > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public static float graceDurationLocalUser = 0.5f;
        public static float graceDurationClient = 1f;
        private List<BanditTimer> hitList = new List<BanditTimer>();
        List<BanditTimer> del = new List<BanditTimer>();
        public class BanditTimer
        {
            public SkillLocator skillLocator;
            public float length;
            public DamageType damageType;
            public CharacterBody body;
            public BanditTimer(CharacterBody b, SkillLocator o, float l, DamageType dt)
            {
                body = b;
                skillLocator = o;
                length = l;
                damageType = dt;
            }
        }
    }
}
