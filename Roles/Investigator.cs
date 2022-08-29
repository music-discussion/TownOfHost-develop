using System.Collections.Generic;
using Hazel;
using UnityEngine;

namespace TownOfHost
{
    public static class Investigator
    {
        private static readonly int Id = 20400;
        public static List<byte> playerIdList = new();

        private static CustomOption KillCooldown;
        private static CustomOption NBareRed;
        private static CustomOption NKareRed;
        private static CustomOption NEareRed;
        private static CustomOption CrewKillingRed;
        private static CustomOption CovenIsPurple;

        public static Dictionary<byte, float> ShotLimit = new();
        public static Dictionary<byte, float> CurrentKillCooldown = new();
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.Investigator);
            KillCooldown = CustomOption.Create(Id + 10, Color.white, "SheriffKillCooldown", 30, 0, 990, 1, Options.CustomRoleSpawnChances[CustomRoles.Investigator]);
            NBareRed = CustomOption.Create(Id + 11, Color.white, "SheriffCanKillArsonist", true, Options.CustomRoleSpawnChances[CustomRoles.Investigator]);
            NKareRed = CustomOption.Create(Id + 12, Color.white, "SheriffCanKillMadmate", true, Options.CustomRoleSpawnChances[CustomRoles.Investigator]);
            NEareRed = CustomOption.Create(Id + 13, Color.white, "SheriffCanKillJester", true, Options.CustomRoleSpawnChances[CustomRoles.Investigator]);
            CrewKillingRed = CustomOption.Create(Id + 14, Color.white, "SheriffCanKillTerrorist", true, Options.CustomRoleSpawnChances[CustomRoles.Investigator]);
            CovenIsPurple = CustomOption.Create(Id + 15, Color.white, "SheriffCanKillOpportunist", true, Options.CustomRoleSpawnChances[CustomRoles.Investigator]);
        }
        public static void Init()
        {
            playerIdList = new();
            ShotLimit = new();
            CurrentKillCooldown = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            CurrentKillCooldown.Add(playerId, KillCooldown.GetFloat());

            if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);

            ShotLimit.TryAdd(playerId, ShotLimitOpt.GetFloat());
            Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 残り{ShotLimit[playerId]}発", "Sheriff");
        }
        public static bool IsEnable => playerIdList.Count > 0;
        private static void SendRPC(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetSheriffShotLimit, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(ShotLimit[playerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            byte SheriffId = reader.ReadByte();
            float Limit = reader.ReadSingle();
            if (ShotLimit.ContainsKey(SheriffId))
                ShotLimit[SheriffId] = Limit;
            else
                ShotLimit.Add(SheriffId, ShotLimitOpt.GetFloat());
        }
        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CurrentKillCooldown[id];
        public static bool CanUseKillButton(PlayerControl player)
        {
            if (player.Data.IsDead)
                return false;

            return true;
        }
        public static bool OnCheckMurder(PlayerControl killer, PlayerControl target, string Process)
        {
            switch (Process)
            {
                case "RemoveShotLimit":
                    ShotLimit[killer.PlayerId]--;
                    Logger.Info($"{killer.GetNameWithRole()} : 残り{ShotLimit[killer.PlayerId]}発", "Sheriff");
                    SendRPC(killer.PlayerId);
                    //SwitchToCorrupt(killer, target);
                    break;
                case "Suicide":
                    if (!target.CanBeKilledBySheriff())
                    {
                        PlayerState.SetDeathReason(killer.PlayerId, PlayerState.DeathReason.Misfire);
                        killer.RpcMurderPlayer(killer);
                        if (CanKillCrewmatesAsIt.GetBool())
                            killer.RpcMurderPlayer(target);
                        return false;
                    }
                    break;
            }
            return true;
        }
        public static bool CanBeKilledBySheriff(this PlayerControl player)
        {
            var cRole = player.GetCustomRole();
            return cRole switch
            {
                CustomRoles.Jester => CanKillJester.GetBool(),
                CustomRoles.Terrorist => CanKillTerrorist.GetBool(),
                CustomRoles.Executioner => CanKillExecutioner.GetBool(),
                CustomRoles.Opportunist => CanKillOpportunist.GetBool(),
                CustomRoles.Arsonist => CanKillArsonist.GetBool(),
                CustomRoles.Egoist => CanKillEgoist.GetBool(),
                CustomRoles.EgoSchrodingerCat => CanKillEgoShrodingerCat.GetBool(),
                CustomRoles.Jackal => CanKillJackal.GetBool(),
                CustomRoles.JSchrodingerCat => CanKillJShrodingerCat.GetBool(),
                CustomRoles.PlagueBearer => CanKillPlagueBearer.GetBool(),
                CustomRoles.Juggernaut => CanKillJug.GetBool(),
                CustomRoles.Vulture => CanKillVulture.GetBool(),
                CustomRoles.TheGlitch => CanKillGlitch.GetBool(),
                CustomRoles.Werewolf => CanKillWerewolf.GetBool(),
                // COVEN //
                CustomRoles.Coven => SheriffCanKillCoven.GetBool(),
                CustomRoles.CovenWitch => SheriffCanKillCoven.GetBool(),
                CustomRoles.Poisoner => SheriffCanKillCoven.GetBool(),
                CustomRoles.HexMaster => SheriffCanKillCoven.GetBool(),
                CustomRoles.PotionMaster => SheriffCanKillCoven.GetBool(),
                CustomRoles.Medusa => SheriffCanKillCoven.GetBool(),
                CustomRoles.Mimic => SheriffCanKillCoven.GetBool(),
                CustomRoles.Necromancer => SheriffCanKillCoven.GetBool(),
                CustomRoles.Conjuror => SheriffCanKillCoven.GetBool(),
                // AFTER COVEN //
                CustomRoles.SchrodingerCat => true,
                CustomRoles.Hacker => true,
                _ => cRole.GetRoleType() switch
                {
                    RoleType.Impostor => true,
                    RoleType.Madmate => CanKillMadmate.GetBool(),
                    _ => false,
                }
            };
        }
    }
}