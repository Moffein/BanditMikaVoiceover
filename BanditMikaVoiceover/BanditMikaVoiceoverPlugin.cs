using BanditMikaVoiceover.Components;
using BepInEx;
using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.Audio;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace BanditMikaVoiceover
{
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Foxyz.BanditMika")]
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.Schale.BanditMikaVoiceover", "BanditMikaVoiceover", "0.1.0")]
    [R2API.Utils.R2APISubmoduleDependency(nameof(SoundAPI), nameof(ContentAddition))]
    public class BanditMikaVoiceoverPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> enableVoicelines;
        private static SurvivorDef banditSurvivorDef = Addressables.LoadAssetAsync<SurvivorDef>("RoR2/Base/Bandit2/Bandit2.asset").WaitForCompletion();
        public static bool playedSeasonalVoiceline = false;
        public static AssetBundle assetBundle;

        public void Awake()
        {
            BaseVoiceoverComponent.Init();
            RoR2.RoR2Application.onLoad += OnLoad;

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BanditMikaVoiceover.banditmikavoiceoverbundle"))
            {
                assetBundle = AssetBundle.LoadFromStream(stream);
            }

            using (var bankStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BanditMikaVoiceover.BanditMikaSoundbank.bnk"))
            {
                var bytes = new byte[bankStream.Length];
                bankStream.Read(bytes, 0, bytes.Length);
                R2API.SoundAPI.SoundBanks.Add(bytes);
            }

            InitNSE();

            enableVoicelines = base.Config.Bind<bool>(new ConfigDefinition("Settings", "Enable Voicelines"), true, new ConfigDescription("Enable voicelines when using the Bandit Mika Skin."));
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
            {
                RiskOfOptionsCompat();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void RiskOfOptionsCompat()
        {
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.CheckBoxOption(enableVoicelines));
            RiskOfOptions.ModSettingsManager.SetModIcon(assetBundle.LoadAsset<Sprite>("texModIcon"));
        }

        private void OnLoad()
        {
            bool foundSkin = false;
            SkinDef[] commandoSkins = SkinCatalog.FindSkinsForBody(BodyCatalog.FindBodyIndex("CommandoBody"));
            foreach (SkinDef skinDef in commandoSkins)
            {
                //nameToken is "ALICKET_SKIN_BAHYCSKINDEFINITION_NAME"
                if (skinDef.name == "BanditMikaDef")
                {
                    foundSkin = true;
                    BanditMikaVoiceoverComponent.requiredSkinDefs.Add(skinDef);
                    break;
                }
            }

            if (!foundSkin)
            {
                Debug.LogError("BanditMikaVoiceover: Bandit Mika SkinDef not found. Voicelines will not work!");
            }
            else
            {
                On.RoR2.CharacterBody.Start += AttachVoiceoverComponent;

                On.RoR2.SurvivorMannequins.SurvivorMannequinSlotController.RebuildMannequinInstance += (orig, self) =>
                {
                    orig(self);
                    if (self.currentSurvivorDef == banditSurvivorDef)
                    {
                        //Loadout isn't loaded first time this is called, so we need to manually get it.
                        //Probably not the most elegant way to resolve this.
                        if (self.loadoutDirty)
                        {
                            if (self.networkUser)
                            {
                                self.networkUser.networkLoadout.CopyLoadout(self.currentLoadout);
                            }
                        }

                        //Check SkinDef
                        BodyIndex bodyIndexFromSurvivorIndex = SurvivorCatalog.GetBodyIndexFromSurvivorIndex(self.currentSurvivorDef.survivorIndex);
                        int skinIndex = (int)self.currentLoadout.bodyLoadoutManager.GetSkinIndex(bodyIndexFromSurvivorIndex);
                        SkinDef safe = HG.ArrayUtils.GetSafe<SkinDef>(BodyCatalog.GetBodySkins(bodyIndexFromSurvivorIndex), skinIndex);
                        if (BanditMikaVoiceoverComponent.requiredSkinDefs.Contains(safe))
                        {
                            bool played = false;
                            if (!playedSeasonalVoiceline)
                            {
                                if (System.DateTime.Today.Month == 1 && System.DateTime.Today.Day == 1)
                                {
                                    Util.PlaySound("Play_BanditMika_Lobby_Newyear", self.mannequinInstanceTransform.gameObject);
                                    played = true;
                                }
                                else if (System.DateTime.Today.Month == 5 && System.DateTime.Today.Day == 8)
                                {
                                    Util.PlaySound("Play_BanditMika_Lobby_bday", self.mannequinInstanceTransform.gameObject);
                                    played = true;
                                }
                                else if (System.DateTime.Today.Month == 10 && System.DateTime.Today.Day == 31)
                                {
                                    Util.PlaySound("Play_BanditMika_Lobby_Halloween", self.mannequinInstanceTransform.gameObject);
                                    played = true;
                                }
                                else if (System.DateTime.Today.Month == 12 && System.DateTime.Today.Day == 25)
                                {
                                    Util.PlaySound("Play_BanditMika_Lobby_xmas", self.mannequinInstanceTransform.gameObject);
                                    played = true;
                                }

                                if (played)
                                {
                                    playedSeasonalVoiceline = true;
                                }
                            }
                            if (!played)
                            {
                                if (Util.CheckRoll(5f))
                                {
                                    Util.PlaySound("Play_BanditMika_TitleDrop", self.mannequinInstanceTransform.gameObject);
                                }
                                else
                                {
                                    Util.PlaySound("Play_BanditMika_Lobby", self.mannequinInstanceTransform.gameObject);
                                }
                            }
                        }
                    }
                };
            }
            BanditMikaVoiceoverComponent.ScepterIndex = ItemCatalog.FindItemIndex("ITEM_ANCIENT_SCEPTER");
        }

        private void InitNSE()
        {
            BanditMikaVoiceoverComponent.nseBlock = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            BanditMikaVoiceoverComponent.nseBlock.eventName = "Play_BanditMika_Blocked";
            R2API.ContentAddition.AddNetworkSoundEventDef(BanditMikaVoiceoverComponent.nseBlock);

            BanditMikaVoiceoverComponent.nseShrineFail = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            BanditMikaVoiceoverComponent.nseShrineFail.eventName = "Play_BanditMika_ShrineFail";
            R2API.ContentAddition.AddNetworkSoundEventDef(BanditMikaVoiceoverComponent.nseShrineFail);

            BanditMikaVoiceoverComponent.nseShout = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            BanditMikaVoiceoverComponent.nseShout.eventName = "Play_BanditMika_Shout";
            R2API.ContentAddition.AddNetworkSoundEventDef(BanditMikaVoiceoverComponent.nseShout);

            BanditMikaVoiceoverComponent.nseSpecial = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            BanditMikaVoiceoverComponent.nseSpecial.eventName = "Play_BanditMika_TacticalAction";
            R2API.ContentAddition.AddNetworkSoundEventDef(BanditMikaVoiceoverComponent.nseSpecial);
        }


        private void AttachVoiceoverComponent(On.RoR2.CharacterBody.orig_Start orig, CharacterBody self)
        {
            orig(self);
            if (self)
            {
                if (BanditMikaVoiceoverComponent.requiredSkinDefs.Contains(SkinCatalog.GetBodySkinDef(self.bodyIndex, (int)self.skinIndex)))
                {
                    BaseVoiceoverComponent existingVoiceoverComponent = self.GetComponent<BaseVoiceoverComponent>();
                    if (!existingVoiceoverComponent) self.gameObject.AddComponent<BanditMikaVoiceoverComponent>();
                }
            }
        }
    }
}
