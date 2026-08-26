using UnityEngine;

public class TruckReference : MonoBehaviour
{
    [SerializeField] private TruckStatus TruckStatus_Main;
    [SerializeField] private TruckController TruckController_Main;
    [SerializeField] private TruckInput TruckInput_Main;

    public TruckStatus Status { get { return TruckStatus_Main; } }
    public TruckController Controller { get { return TruckController_Main; } }
    public TruckInput Input { get { return TruckInput_Main; } }
    public Transform BodyTransform { get { return TruckStatus_Main.transform; } }
}