using RoR2;
using UnityEngine;

namespace BanditTweaks
{
    class QuickdrawComponent : MonoBehaviour
    {
        SkillLocator sk;

        private int prevSecondaryStock = 0;
        private int prevUtilityStock = 0;
        private int prevSpecialStock = 0;

        public void Awake()
        {
            sk = base.GetComponent<SkillLocator>();
            if (!sk)
            {
                Destroy(this);
            }
        }

        public void FixedUpdate()
        {
            if (BanditTweaks.quickdrawEnabled)
            {
                if (sk.secondary.stock < prevSecondaryStock || sk.utility.stock < prevUtilityStock || sk.special.stock < prevSpecialStock)
                {
                    sk.primary.stock = sk.primary.maxStock;
                }
                prevSecondaryStock = sk.secondary.stock;
                prevUtilityStock = sk.utility.stock;
                prevSpecialStock = sk.special.stock;
            }
            if (sk.primary.stock > sk.primary.maxStock)
            {
                sk.primary.stock = sk.primary.maxStock;
            }
        }
    }
}
