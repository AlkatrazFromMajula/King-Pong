using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerCard : MonoBehaviour
{
    #region Fields

    [SerializeField] List<Sprite> phaseOneScores;
    [SerializeField] List<Sprite> phaseTwoScores;
    [SerializeField] List<Sprite> iconsNormal;
    [SerializeField] List<Sprite> iconsHighlighted;
    Utils.Opponent opponent;
    GameOverEvent gameOverEvent;
    VictoryEvent victoryEvent;
    PhaseTwoEvent phaseTwoEvent;
    SpriteRenderer currentIcon;
    SpriteRenderer currentScore;
    Utils utils;
    private int score;
    private int characterIndex;

    #endregion

    #region Private Methods

    private void Awake()
    {
        // set references
        utils = FindObjectOfType<Utils>();
        gameOverEvent = new GameOverEvent();
        victoryEvent = new VictoryEvent();
        phaseTwoEvent = new PhaseTwoEvent();
        currentIcon = GetComponentsInChildren<SpriteRenderer>()[1];
        currentScore = GetComponentsInChildren<SpriteRenderer>()[2];

        // first of all set self as an invoker of multiple events
        opponent = transform.position.y >= 0 ? Utils.Opponent.North : Utils.Opponent.South;
        if (opponent == Utils.Opponent.South) { utils.SetVictoryEventInvoker(this); utils.SetPhaseTwoEventInvoker(this); }
        else { utils.SetGameOverEventInvoker(this); }
    }

    // Start is called before the first frame update
    private void Start()
    {
        // add self as listener of multiple events
        utils.AddGoalEventListner(UpdateScore);
        utils.AddBeginGameEventListner(ResetScore);

        // set default sprites based on chosen character
        switch (opponent == Utils.Opponent.North ? DataManager.Instance.northCharacter : DataManager.Instance.southCharacter)
        {
            case Utils.Character.Peasant: characterIndex = 0; break;
            default: characterIndex = 1; break;
        }
        currentIcon.sprite = iconsNormal[characterIndex];
        ResetScore();
    }

    // Reset score to zero points
    private void ResetScore() { score = 0; currentScore.sprite = utils.phaseTwo ? phaseTwoScores[0] : phaseOneScores[0]; }

    // React to goal event and update score
    private void UpdateScore(Utils.Opponent opponent)
    {
        // update if it's going about this card
        if (opponent == this.opponent)
        {
            // update score 
            if (score < 3) { ++score; currentScore.sprite = utils.phaseTwo ? phaseTwoScores[score] : phaseOneScores[score]; }

            // if one of opponents wins, invoke corresponding event
            if (this.opponent == Utils.Opponent.North && score == 3) { gameOverEvent.Invoke(); }
            else if (this.opponent == Utils.Opponent.South && score == 3 && !utils.phaseTwo) { phaseTwoEvent.Invoke(); utils.phaseTwo = true; }
            else if (this.opponent == Utils.Opponent.South && score == 3 && utils.phaseTwo) { victoryEvent.Invoke(); }
        }
    }

    // Start blinking icon
    private IEnumerator Blink()
    {
        while (true)
        {
            currentIcon.sprite = iconsHighlighted[characterIndex];
            yield return new WaitForSeconds(0.25f);

            currentIcon.sprite = iconsNormal[characterIndex];
            yield return new WaitForSeconds(0.25f);
        }
    }

    #endregion

    #region Public Methods

    public void AddGameOverEventListener(UnityAction listener)
    {
        gameOverEvent.AddListener(listener);
    }

    public void AddPhaseTwoEventListener(UnityAction listener)
    {
        phaseTwoEvent.AddListener(listener);
    }

    public void AddVictoryEventListener(UnityAction listener)
    {
        victoryEvent.AddListener(listener);
    }

    /// <summary>
    /// If "blink" is set to true, makes character's icon blink, otherwise makes it stop blinking
    /// </summary>
    public void HighlightIcon(bool blink)
    {
        if (blink) { StartCoroutine(Blink()); }
        else { StopAllCoroutines(); currentIcon.sprite = iconsNormal[characterIndex]; }
    }

    #endregion
}
