using Dman.LSystem.SystemRuntime.Turtle;
using Dman.LSystem.UnityObjects;
using UnityEngine;

[RequireComponent(typeof(LSystemBehavior))]
[RequireComponent(typeof(TurtleInterpreterBehavior))]
public class LindenmayerStatisticsGatherer : MonoBehaviour
{
    private LSystemBehavior lsystem;
    private TurtleInterpreterBehavior turtle;

    public LineGraph lsystemOrganCounts;
    public LineGraph boundingBoxSurfaceArea;

    void Awake()
    {
        lsystem = GetComponent<LSystemBehavior>();
        turtle = GetComponent<TurtleInterpreterBehavior>();
        
        lsystem.OnSystemStateUpdated += OnLsystemStateUpdated;
        turtle.OnTurtleMeshUpdated += OnTurtleMeshUpdated;
    }

    private void OnDestroy()
    {
        lsystem.OnSystemStateUpdated -= OnLsystemStateUpdated;
        turtle.OnTurtleMeshUpdated -= OnTurtleMeshUpdated;
    }

    private void OnLsystemStateUpdated()
    {
        var symbols = lsystem.steppingHandle.currentState.currentSymbols;
        var symbolCount = symbols.Data.Length;
        lsystemOrganCounts.AppendData(symbolCount);
    }
    private void OnTurtleMeshUpdated()
    {
        var boundsSize = turtle.GetComponent<MeshRenderer>().bounds.size;
        var area = boundsSize.x * boundsSize.y + boundsSize.x * boundsSize.z + boundsSize.y * boundsSize.z;
        boundingBoxSurfaceArea.AppendData(area * 2);
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
