using System;
using Unity.Netcode;
using UnityEngine;

public enum LevelUpCandidateKind : byte
{
    None,
    GeneralUpgrade,
    Skill
}

public interface ILevelUpChoiceSource
{
    bool ChoiceActive { get; }
    bool HasSelected { get; }
    int ChoiceTeamLevel { get; }
    int ChoiceSessionId { get; }
    double ChoiceDeadline { get; }
    int CandidateCount { get; }
    LevelUpSettings Settings { get; }

    LevelUpCandidateState GetCandidate(int index);
    SkillData GetSkillData(int poolIndex);
    int GetCurrentSkillLevel(int poolIndex);
    string GetSelectionStatusText();
    void RequestChoice(int index);
}

public struct LevelUpCandidateState :
    INetworkSerializable,
    IEquatable<LevelUpCandidateState>
{
    public LevelUpCandidateKind Kind;
    public GeneralUpgradeType GeneralUpgrade;
    public int SkillPoolIndex;
    public int TargetSkillLevel;
    public ElementType Element;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref Kind);
        serializer.SerializeValue(ref GeneralUpgrade);
        serializer.SerializeValue(ref SkillPoolIndex);
        serializer.SerializeValue(ref TargetSkillLevel);
        serializer.SerializeValue(ref Element);
    }

    public bool Equals(LevelUpCandidateState other)
    {
        return Kind == other.Kind &&
            GeneralUpgrade == other.GeneralUpgrade &&
            SkillPoolIndex == other.SkillPoolIndex &&
            TargetSkillLevel == other.TargetSkillLevel &&
            Element == other.Element;
    }
}

public struct PlayerGrowthNetworkState :
    INetworkSerializable,
    IEquatable<PlayerGrowthNetworkState>
{
    public int TeamLevel;
    public float DamageMultiplier;
    public float AttackIntervalMultiplier;
    public float SkillCooldownMultiplier;
    public float MoveSpeedMultiplier;
    public float ProjectileSpeedMultiplier;
    public int ProjectileCountBonus;
    public int BasicAttackBounceCount;

    public static PlayerGrowthNetworkState Default
    {
        get
        {
            return new PlayerGrowthNetworkState
            {
                TeamLevel = 1,
                DamageMultiplier = 1f,
                AttackIntervalMultiplier = 1f,
                SkillCooldownMultiplier = 1f,
                MoveSpeedMultiplier = 1f,
                ProjectileSpeedMultiplier = 1f
            };
        }
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref TeamLevel);
        serializer.SerializeValue(ref DamageMultiplier);
        serializer.SerializeValue(ref AttackIntervalMultiplier);
        serializer.SerializeValue(ref SkillCooldownMultiplier);
        serializer.SerializeValue(ref MoveSpeedMultiplier);
        serializer.SerializeValue(ref ProjectileSpeedMultiplier);
        serializer.SerializeValue(ref ProjectileCountBonus);
        serializer.SerializeValue(ref BasicAttackBounceCount);
    }

    public bool Equals(PlayerGrowthNetworkState other)
    {
        return TeamLevel == other.TeamLevel &&
            DamageMultiplier.Equals(other.DamageMultiplier) &&
            AttackIntervalMultiplier.Equals(other.AttackIntervalMultiplier) &&
            SkillCooldownMultiplier.Equals(other.SkillCooldownMultiplier) &&
            MoveSpeedMultiplier.Equals(other.MoveSpeedMultiplier) &&
            ProjectileSpeedMultiplier.Equals(other.ProjectileSpeedMultiplier) &&
            ProjectileCountBonus == other.ProjectileCountBonus &&
            BasicAttackBounceCount == other.BasicAttackBounceCount;
    }
}

public struct SkillProgressNetworkState :
    INetworkSerializable,
    IEquatable<SkillProgressNetworkState>
{
    public int SkillPoolIndex;
    public int Level;
    public ElementType Element;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref SkillPoolIndex);
        serializer.SerializeValue(ref Level);
        serializer.SerializeValue(ref Element);
    }

    public bool Equals(SkillProgressNetworkState other)
    {
        return SkillPoolIndex == other.SkillPoolIndex &&
            Level == other.Level &&
            Element == other.Element;
    }
}

public struct SkillCastRuntime : INetworkSerializable
{
    public float Damage;
    public float Range;
    public float ProjectileSpeed;
    public float Cooldown;
    public int SkillLevel;
    public int ProjectileCount;
    public float ProjectileSpreadAngle;
    public int PierceBonus;
    public float ExplosionRadiusMultiplier;
    public float ActivationIntervalMultiplier;
    public float ZoneRadiusMultiplier;
    public float ZoneDurationMultiplier;
    public float MovementSpeedMultiplier;
    public float ZoneDamageMultiplier;
    public int BounceCount;
    public float BounceRange;

    public static SkillCastRuntime FromBase(SkillData skill)
    {
        return new SkillCastRuntime
        {
            Damage = skill != null ? skill.Damage : 0f,
            Range = skill != null ? skill.Range : 0f,
            ProjectileSpeed = skill != null ? skill.ProjectileSpeed : 0f,
            Cooldown = skill != null ? skill.Cooldown : 1f,
            SkillLevel = 1,
            ProjectileCount = 1,
            ExplosionRadiusMultiplier = 1f,
            ActivationIntervalMultiplier = 1f,
            ZoneRadiusMultiplier = 1f,
            ZoneDurationMultiplier = 1f,
            MovementSpeedMultiplier = 1f,
            ZoneDamageMultiplier = 1f
        };
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref Damage);
        serializer.SerializeValue(ref Range);
        serializer.SerializeValue(ref ProjectileSpeed);
        serializer.SerializeValue(ref Cooldown);
        serializer.SerializeValue(ref SkillLevel);
        serializer.SerializeValue(ref ProjectileCount);
        serializer.SerializeValue(ref ProjectileSpreadAngle);
        serializer.SerializeValue(ref PierceBonus);
        serializer.SerializeValue(ref ExplosionRadiusMultiplier);
        serializer.SerializeValue(ref ActivationIntervalMultiplier);
        serializer.SerializeValue(ref ZoneRadiusMultiplier);
        serializer.SerializeValue(ref ZoneDurationMultiplier);
        serializer.SerializeValue(ref MovementSpeedMultiplier);
        serializer.SerializeValue(ref ZoneDamageMultiplier);
        serializer.SerializeValue(ref BounceCount);
        serializer.SerializeValue(ref BounceRange);
    }
}
