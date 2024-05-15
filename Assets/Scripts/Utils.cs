using UnityEngine;
using UnityEngine.Events;
using System;

// This class is first of all responsible for declaring and distributing utils, but also for event management 
public class Utils : MonoBehaviour
{
    #region Fields

    public ArenaProperties arenaProperties;
    public BallProperties ballProperties;
    public CharacterProperties characterProperties;
    public ShieldProperties shieldProperties;
    public AbilityProperties abilityProperties;
    public AIProperties aIProperties;

    [Serializable]
    public struct ArenaProperties
    {
        [SerializeField] private float arenaWidth;
        [SerializeField] private float arenaHeight;
        [SerializeField] private float defaultCharacterOffsetY;
        [SerializeField] private float characterCageWidth;

        public float ArenaWidth => arenaWidth;
        public float ArenaHeight => arenaHeight;
        public float DefaultCharacterOffsetY => defaultCharacterOffsetY;
        public float CharacterCageWidth => characterCageWidth;
    }

    [Serializable]
    public struct BallProperties
    {
        [SerializeField] private float ballStartVelocity;
        [SerializeField] private float ballMaxVelocity;
        [SerializeField] private float ballAcceleration;
        [SerializeField] private float ballRadius;
        [SerializeField] private Vector2 startBallDirection;
        [SerializeField] private float ballBounceAngle;

        public float BallStartVelocity => ballStartVelocity;
        public float BallMaxVelocity => ballMaxVelocity;
        public float BallAcceleration => ballAcceleration;
        public float BallRadius => ballRadius;
        public Vector2 StartBallDirection => startBallDirection;
        public float BallBounceAngle => ballBounceAngle;
    }

    [Serializable]
    public struct CharacterProperties
    {
        [SerializeField] private float halthCharacteridth;
        [SerializeField] private float characterVelocity;
        [SerializeField] private float parryTime;
        [SerializeField] private float parryCountdownTime;

        public float HalthCharacteridth => halthCharacteridth;
        public float CharacterVelocity => characterVelocity;
        public float ParryTime => parryTime;
        public float ParryCountdownTime => parryCountdownTime;
    }

    [Serializable]
    public struct ShieldProperties
    {
        [SerializeField] private float shieldOffset;
        [SerializeField] private int wheelEndurance;
        [SerializeField] private int plankEndurance;
        [SerializeField] private int hayEndurance;
        [SerializeField] private int royalEndurance;

        public float ShieldOffset => shieldOffset;
        public int WheelEndurance => wheelEndurance;
        public int PlankEndurance => plankEndurance;
        public int HayEndurance => hayEndurance;
        public int RoyalEndurance => royalEndurance;
    }

    [Serializable]
    public struct AbilityProperties
    {
        [SerializeField] private float royalOrderCooldownTime;
        [SerializeField] private float mightyPunchCooldownTime;

        public float RoyalOrderCooldownTime => royalOrderCooldownTime;
        public float MightyPunchCooldownTime => mightyPunchCooldownTime;
    }

    [Serializable]
    public struct AIProperties
    {
        [SerializeField] private float hitBallVelocity_AI;
        [SerializeField] private float defaultVelocity_AI;

        public float HitBallVelocity_AI => hitBallVelocity_AI;
        public float DefaultVelocity_AI => defaultVelocity_AI;
    }

    // general
    public bool phaseTwo = false;

    // event management
    private Ball goalEventInvoker;
    private Ball hitBorderEventInvoker;
    private PlayerCard gameOverEventInvoker;
    private PlayerCard victoryEventInvoker;
    private PlayerCard phaseTwoEventInvoker;
    private Referee beginGameEventInvoker;
    private Menu totalMuteEventInvoker;

    #endregion

    #region EventManager

    /// <summary>
    /// Adds given action as a listener of hit border event 
    /// </summary>
    /// <param name="listener"> new listener of hit border event </param>
    public void AddHitBorderEventListner(UnityAction<Arena.Border> listener) { hitBorderEventInvoker.AddHitBorderEventListener(listener); }

    /// <summary>
    /// Adds given ball as an invoker of hit border event 
    /// </summary>
    /// <param name="invoker"> ball invoker of hit border event </param>
    public void SetHitBorderEventInvoker(Ball invoker) { hitBorderEventInvoker = invoker; }

    /// <summary>
    /// Adds given action as a listener of total mute event 
    /// </summary>
    /// <param name="listener"> new listener of total mute event </param>
    public void AddTotalMuteEventListner(UnityAction listener) { totalMuteEventInvoker.AddTotalMuteEventListener(listener); }

    /// <summary>
    /// Adds given ball as an invoker of total mute event 
    /// </summary>
    /// <param name="invoker"> ball invoker of total mute event </param>
    public void SetTotalMuteEventInvoker(Menu invoker) { totalMuteEventInvoker = invoker; }

    /// <summary>
    /// Adds given action as a listener of goal event 
    /// </summary>
    /// <param name="listener"> new listener of goal event </param>
    public void AddGoalEventListner(UnityAction<Opponent> listener) { goalEventInvoker.AddGoalEventListener(listener); }

    /// <summary>
    /// Removes given action as a listener of goal event 
    /// </summary>
    /// <param name="listener"> former listener of goal event </param>
    public void RemoveGoalEventListner(UnityAction<Opponent> listener) { goalEventInvoker.RemoveGoalEventListener(listener); }


    /// <summary>
    /// Adds given ball as an invoker of goal event 
    /// </summary>
    /// <param name="invoker"> ball invoker of goal event </param>
    public void SetGoalEventInvoker(Ball invoker) { goalEventInvoker = invoker; }

    /// <summary>
    /// Adds given action as a listener of game over event 
    /// </summary>
    /// <param name="listener"> new listener of game over event </param>
    public void AddGameOverEventListner(UnityAction listener) { gameOverEventInvoker.AddGameOverEventListener(listener); }

    /// <summary>
    /// Adds given ball as an invoker of game over event 
    /// </summary>
    /// <param name="invoker"> ball invoker of game over event </param>
    public void SetGameOverEventInvoker(PlayerCard invoker) { gameOverEventInvoker = invoker; }

    /// <summary>
    /// Adds given action as a listener of victory event 
    /// </summary>
    /// <param name="listener"> new listener of victory event </param>
    public void AddVictoryEventListner(UnityAction listener) { victoryEventInvoker.AddVictoryEventListener(listener); }

    /// <summary>
    /// Adds given ball as an invoker of victory event 
    /// </summary>
    /// <param name="invoker"> ball invoker of victory event </param>
    public void SetVictoryEventInvoker(PlayerCard invoker) { victoryEventInvoker = invoker; }

    /// <summary>
    /// Adds given action as a listener of phase two event 
    /// </summary>
    /// <param name="listener"> new listener of phase two event </param>
    public void AddPhaseTwoEventListner(UnityAction listener) { phaseTwoEventInvoker.AddPhaseTwoEventListener(listener); }

    /// <summary>
    /// Adds given ball as an invoker of phase two event 
    /// </summary>
    /// <param name="invoker"> ball invoker of phase two event </param>
    public void SetPhaseTwoEventInvoker(PlayerCard invoker) { phaseTwoEventInvoker = invoker; }

    /// <summary>
    /// Adds given action as a listener of beginGame event 
    /// </summary>
    /// <param name="listener"> new listener of beginGame event </param>
    public void AddBeginGameEventListner(UnityAction listener) { beginGameEventInvoker.AddBeginGameEventListener(listener); }

    /// <summary>
    /// Adds given ball as an invoker of beginGame event 
    /// </summary>
    /// <param name="invoker"> ball invoker of beginGame event </param>
    public void SetBeginGameEventInvoker(Referee invoker) { beginGameEventInvoker = invoker; }

    #endregion

    #region Public Getters

    /// <summary>
    /// Enumerates all possible characters
    /// </summary>
    public enum Character { King, Peasant, None }

    /// <summary>
    /// Enumerates two oppenents
    /// </summary>
    public enum Opponent { North, South }

    /// <summary>
    /// Enumerates possible shield types
    /// </summary>
    public enum ShieldType { Wheel, Plank, Hay, Royal }

    /// <summary>
    /// Provides given character's prefab
    /// </summary>
    /// <param name="character"> Character's name </param>
    /// <param name="isPlayer"> Is going to be used by player or AI </param>
    /// <returns> Requested character to be used by player if "isPlayer" is set to true, otherwise to be used by AI </returns>
    public GameObject GetCharacterPrefab(Character character, bool isPlayer) 
    {
        switch (character) 
        {
            case Character.King: return null;
            default: return null;
        }
    }

    /// <summary>
    /// Gets shield type-specific endurance
    /// </summary>
    /// <param name="shieldType"> The shield type </param>
    /// <returns> An integer representation of shield's endurance </returns>
    public int GetShieldEndurance(ShieldType shieldType)
    {
        switch (shieldType)
        {
            case ShieldType.Wheel: return shieldProperties.WheelEndurance;
            case ShieldType.Plank: return shieldProperties.PlankEndurance;
            case ShieldType.Hay: return shieldProperties.HayEndurance;
            case ShieldType.Royal: return shieldProperties.RoyalEndurance;
            default: return 0;
        }
    }

    public float GetAbilityCooldownTime(Ability.AbilityType abilityType)
    {
        switch (abilityType)
        {
            case Ability.AbilityType.RoyalOrder : return abilityProperties.RoyalOrderCooldownTime;
            case Ability.AbilityType.MightyPunch: return abilityProperties.MightyPunchCooldownTime;
            default: return 0;
        }
    }

    #endregion

    #region Useful

    /// <summary>
    /// Compares ability and shield types  
    /// </summary>
    /// <param name="shieldType"> Shield type </param>
    /// <param name="abilityType"> Ability type </param>
    /// <returns> True if ability and shield suit eachother, otherwise false </returns>
    public bool CompareShieldWithAbility(ShieldType shieldType, Ability.AbilityType abilityType) 
    {
        return (
            (shieldType == ShieldType.Wheel && abilityType == Ability.AbilityType.MightyPunch) ||
            (shieldType == ShieldType.Royal && abilityType == Ability.AbilityType.RoyalOrder));
    }

    #endregion

}
