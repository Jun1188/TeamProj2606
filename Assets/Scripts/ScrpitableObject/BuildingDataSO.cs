using UnityEngine;

[CreateAssetMenu(fileName = "NewBuilding", menuName = "ScriptableObjects/Building")]
public class BuildingDataSO : ItemDataSO
{
     
    public GameObject prefab;

    public Vector2Int size = Vector2Int.one; // 타일 크기

    public BuildingCategory category;

    // 포트 정의: 배치 후 자동 연결의 핵심
    //public PortDefinition[] ports;

    public RecipeDataSO[] availableRecipes;
    public float processingTime;
    public int maxInputBuffer, maxOutputBuffer;

    public enum BuildingCategory
    {
        Smelter,
        Assembler,
        Storage,
        Conveyor,
        PowerGenerator,
        PowerConsumer,
        Other

    }
}