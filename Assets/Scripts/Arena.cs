using UnityEngine;

public class Arena : MonoBehaviour
{
    // references
    Animator animator;
    Utils utils;

    // set references
    private void Awake() { animator = GetComponent<Animator>(); utils = FindObjectOfType<Utils>(); }

    void Start()
    {
        // disable effects by default
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>()) { renderer.enabled = false; }

        // add self as listener of multiple events
        utils.AddGoalEventListner(Goal);
        utils.AddHitBorderEventListner(HitBorder);
    }

    /// <summary>
    /// Left and right arena borders
    /// </summary>
    public enum Border { West, East }

    // blink backlights 
    private void Goal(Utils.Opponent opponent) { animator.Play(opponent == Utils.Opponent.North ? "A_Arena_Goal_South" : "A_Arena_Goal_North", 0); }

    // highlight border 
    private void HitBorder(Border border) { animator.Play(border == Border.West ? "A_Arena_HitBorder_West" : "A_Arena_HitBorder_East", 0); }

}
