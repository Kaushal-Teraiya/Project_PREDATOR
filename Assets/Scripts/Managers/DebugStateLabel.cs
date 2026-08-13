using TMPro;
using Unity.Mathematics;
using UnityEngine;

public class DebugStateLabel : MonoBehaviour
{
    [SerializeField] private TextMeshPro text;
    [SerializeField] private EnemyBrain brain;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, 0f);

    void LateUpdate()
    {
        string stateName = brain.GetCurrentState();
        if (stateName == "SearchState")
        {
            stateName += " : " + brain.GetSearchPhase();
        }
        text.text = stateName;
        text.color = GetColor(stateName);

        Vector3 direction = text.transform.position - Camera.main.transform.position;
        direction.y = 0f;
        text.transform.rotation = Quaternion.LookRotation(direction);
    }

    private Color GetColor(string state)
    {
        switch (state)
        {
            case "IdleState":
                return Color.yellow;
            case "WanderState":
                return Color.green;
            case "ChaseState":
                return Color.red;
            case "AttackState":
                return Color.magenta;
            case "SearchState":
                return Color.blue;
            case "InvestigateState":
                return Color.gold;
            case "DeadState":
                return Color.black;
            case "BufferState":
                return Color.azure;
            case "RepositionState":
                return Color.rebeccaPurple;
            case "CombatState":
                return Color.aliceBlue;
            default:
                return Color.white;
        }
    }
}
