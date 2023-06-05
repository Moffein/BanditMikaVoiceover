using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace BanditMikaVoiceover.Components
{
    public class BanditMikaVoiceoverComponent : BaseVoiceoverComponent
    {
        public static List<SkinDef> requiredSkinDefs = new List<SkinDef>();
        public static NetworkSoundEventDef nseBlock;
        public static NetworkSoundEventDef nseShrineFail;
        public static NetworkSoundEventDef nseShout;
        public static NetworkSoundEventDef nseSpecial;

        public static ItemIndex ScepterIndex;

        private float blockedCooldown = 0f;
        private float levelCooldown = 0f;
        private float shrineOfChanceFailCooldown = 0f;
        private float specialCooldown = 0f;
        private bool acquiredScepter = false;

        protected override void Awake()
        {
            spawnVoicelineDelay = 3f;
            if (Run.instance && Run.instance.stageClearCount == 0)
            {
                spawnVoicelineDelay = 6.5f;
            }
            base.Awake();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (blockedCooldown > 0f) blockedCooldown -= Time.fixedDeltaTime;
            if (levelCooldown > 0f) levelCooldown -= Time.fixedDeltaTime;
            if (shrineOfChanceFailCooldown > 0f) shrineOfChanceFailCooldown -= Time.fixedDeltaTime;
            if (specialCooldown > 0f) specialCooldown -= Time.fixedDeltaTime;
        }


        public override void PlayDamageBlockedServer()
        {
            if (!NetworkServer.active || blockedCooldown > 0f) return;
            bool played = TryPlayNetworkSound(nseBlock, 2.2f, false);
            if (played) blockedCooldown = 10f;
        }

        public override void PlayDeath()
        {
            TryPlaySound("Play_BanditMika_Defeat", 4.5f, true);
        }

        public override void PlayHurt(float percentHPLost)
        {
            if (percentHPLost >= 0.1f)
            {
                TryPlaySound("Play_BanditMika_TakeDamage", 0f, false);
            }
        }

        public override void PlayJump() { }

        public override void PlayLevelUp()
        {
            if (levelCooldown > 0f) return;
            bool played = TryPlaySound("Play_BanditMika_LevelUp", 10f, false);
            if (played) levelCooldown = 60f;
        }

        public override void PlayLowHealth() { }

        public override void PlayPrimaryAuthority() { }

        public override void PlaySecondaryAuthority()
        {
            TryPlayNetworkSound(nseShout, 0f, false);
        }

        public override void PlaySpawn()
        {
            TryPlaySound("Play_BanditMika_Spawn", 3f, true);
        }

        public override void PlaySpecialAuthority()
        {
            if (specialCooldown > 0f) return;
            bool played = TryPlayNetworkSound(nseSpecial, 1.9f, false);
            if (played) specialCooldown = 30f;
        }

        public override void PlayTeleporterFinish()
        {
            TryPlaySound("Play_BanditMika_Victory", 4f, false);
        }

        public override void PlayTeleporterStart()
        {
            TryPlaySound("Play_BanditMika_TeleporterStart", 4.2f, false);
        }

        public override void PlayUtilityAuthority() { }

        public override void PlayVictory()
        {
            TryPlaySound("Play_BanditMika_Victory", 4f, true);
        }

        protected override void Inventory_onItemAddedClient(ItemIndex itemIndex)
        {
            base.Inventory_onItemAddedClient(itemIndex);
            if (BanditMikaVoiceoverComponent.ScepterIndex != ItemIndex.None && itemIndex == BanditMikaVoiceoverComponent.ScepterIndex)
            {
                PlayAcquireScepter();
            }
            else
            {
                ItemDef id = ItemCatalog.GetItemDef(itemIndex);
                if (itemIndex == RoR2Content.Items.Squid.itemIndex || itemIndex == RoR2Content.Items.Plant.itemIndex)
                {
                    PlayBadItem();
                }
                else if (id && id.deprecatedTier == ItemTier.Tier3)
                {
                    PlayAcquireLegendary();
                }
            }
        }

        public void PlayAcquireScepter()
        {
            if (acquiredScepter) return;
            TryPlaySound("Play_BanditMika_AcquireScepter", 19f, false);
            acquiredScepter = true;
        }

        public void PlayAcquireLegendary()
        {
            TryPlaySound("Play_BanditMika_FindLegendary", 9f, false);
        }

        public void PlayBadItem()
        {
            TryPlaySound("Play_BanditMika_BadItem", 4.2f, false);
        }
    }
}
