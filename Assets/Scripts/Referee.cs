using UnityEngine;
using UnityEngine.Events;

public class Referee : MonoBehaviour
{
    #region Fields

    Utils utils;
    BeginGameEvent beginGameEvent;
    Animator animator;

    #endregion

    #region Private Methods

    private void Awake()
    {
        // set references
        utils = FindObjectOfType<Utils>();
        beginGameEvent = new BeginGameEvent();
        animator = GetComponent<Animator>();

        // first of all add self as invoker of begin game event
        utils.SetBeginGameEventInvoker(this);
    }

    // Start is called before the first frame update
    void Start()
    {
        // add self as a listener of multiple events
        utils.AddPhaseTwoEventListner(PhaseTwo);
        utils.AddGoalEventListner(Goal);
    }

    private void InvokeBeginGameEvent() { beginGameEvent.Invoke(); }  

    // Transfer referee to phase two
    private void PhaseTwo() { animator.Play("A_Referee_PhaseTwo", 0); utils.RemoveGoalEventListner(Goal); }

    // React to goal event
    private void Goal(Utils.Opponent opponent) 
    {
        //  anounce goal if game is in first phase
        if (opponent == Utils.Opponent.North && !utils.phaseTwo) { animator.Play("A_Referee_Goal_North", 0); }
        else if (!utils.phaseTwo) { animator.Play("A_Referee_Goal_South", 0); }
    }

    #endregion

    #region Public Methods

    public void AddBeginGameEventListener(UnityAction listener) { beginGameEvent.AddListener(listener); }

    #endregion
}
