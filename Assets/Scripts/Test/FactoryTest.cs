using System.Text;
using UnityEngine;

public class FactoryTest : MonoBehaviour
{
    [Header("ScriptableObjects — Inspector에서 연결")]
    public ItemDataSO ironOreSO;

    private Camera mainCamera;
    private string currentBuildingInfo = "";

    void Start()
    {
        // 메인 카메라 참조 캐싱
        mainCamera = Camera.main;

        // 기존 테스트용 건물 자동 배치 로직 유지
        MiningService.GetItemAt = _ => ironOreSO;
    }

    void Update()
    {
        // 마우스 좌클릭 감지
        if (Input.GetMouseButtonDown(0))
        {
            DetectAndDisplayBuilding();
        }
    }

    private void DetectAndDisplayBuilding()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        // 마우스 커서 위치로부터 3D 월드로 향하는 레이(Ray) 생성
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // 레이캐스트를 발사하여 오브젝트 충돌 검사
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            // 부딪힌 오브젝트 또는 그 부모에게서 BuildingInstance 컴포넌트가 있는지 탐색
            BuildingInstance clickedBuilding = hit.collider.GetComponentInParent<BuildingInstance>();

            if (clickedBuilding != null)
            {
                // 건물을 찾았다면 정보 출력
                PrintBuildingData(clickedBuilding);
            }
        }
    }

    private void PrintBuildingData(BuildingInstance building)
    {
        StringBuilder sb = new StringBuilder();

        // 1. 건물 기본 정보 서식화
        sb.AppendLine("--------------------------------------------------");
        sb.AppendLine("[ 건물 정보 ]");
        sb.AppendLine($"- 이름 : {building.Data.name}");
        sb.AppendLine($"- 종류 : {building.Data.category}");
        sb.AppendLine($"- 위치 : {building.Origin}");
        sb.AppendLine($"- 회전 : {building.RotationSteps}단계 ({building.RotationSteps * 90}도)");
        sb.AppendLine();

        // 2. 인벤토리 버퍼 정보 서식화
        BuildingInventory inv = building.Inventory;
        if (inv != null)
        {
            sb.AppendLine("[ 버퍼 상태 ]");

            // 입력 버퍼 출력
            var inputs = inv.InputSnapshot;
            sb.AppendLine($"- 입력 버퍼 (최대 용량: {inv.MaxIn}개)");
            if (inputs != null && inputs.Count > 0)
            {
                foreach (var input in inputs)
                {
                    sb.AppendLine($"  * {input.item.name} : {input.n}개");
                }
            }
            else
            {
                sb.AppendLine("  * (비어 있음)");
            }

            sb.AppendLine();

            // 출력 버퍼 출력
            var outputs = inv.OutputSnapshot;
            sb.AppendLine($"- 출력 버퍼 (최대 용량: {inv.MaxOut}개)");
            if (outputs != null && outputs.Count > 0)
            {
                foreach (var output in outputs)
                {
                    sb.AppendLine($"  * {output.item.name} : {output.n}개");
                }
            }
            else
            {
                sb.AppendLine("  * (비어 있음)");
            }
        }
        sb.AppendLine("--------------------------------------------------");

        currentBuildingInfo = sb.ToString();
    }

    void OnGUI()
    {
        GUI.TextArea(new Rect(20, 200, 400, 300), currentBuildingInfo);
    }

    void OnDrawGizmos()
    {
        // 게임이 실행 중일 때만 기즈모 연산 수행
        if (!Application.isPlaying) return;

        // 1. 씬 내의 모든 빌딩 인스턴스 검색
        BuildingInstance[] allBuildings = FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None);
        if (allBuildings == null) return;

        // 2. 모든 건물의 연결선 시각화 (녹색 선)
        Gizmos.color = Color.green;
        foreach (var b in allBuildings)
        {
            if (b == null || b.OutputConnections == null) continue;

            Vector3 startPos = b.transform.position + Vector3.up * 0.5f;

            foreach (var conn in b.OutputConnections)
            {
                if (conn.To == null) continue;

                Vector3 endPos = conn.To.transform.position + Vector3.up * 0.5f;

                // 출력 포트 -> 입력 포트 방향 직선
                Gizmos.DrawLine(startPos, endPos);

                // 흐름 방향 안내용 작은 구체
                Vector3 dir = (endPos - startPos).normalized;
                Gizmos.DrawSphere(endPos - dir * 0.3f, 0.1f);
            }
        }

        // 3. 모든 벨트 세그먼트의 아이템 실시간 위치 시각화 (노란색 구체)
        var segManager = BeltSegmentManager.Instance;
        if (segManager != null && segManager.Segments != null)
        {
            Gizmos.color = Color.yellow;

            foreach (var seg in segManager.Segments)
            {
                if (seg == null || !seg.HasItems) continue;

                foreach (var (item, pos) in seg.Items)
                {
                    // pos 값을 기준으로 현재 어느 벨트 위에 있는지 인덱스 계산 (소수점 버림)
                    int beltIndex = Mathf.FloorToInt(pos);

                    if (beltIndex >= 0 && beltIndex < seg.Belts.Count)
                    {
                        BuildingInstance currentBelt = seg.Belts[beltIndex];
                        if (currentBelt == null) continue;

                        // 현재 벨트 타일 내에서의 진행 비율 (0.0 ~ 1.0)
                        float t = pos - beltIndex;
                        Vector3 itemWorldPos;

                        // 다음 연속된 벨트가 존재한다면 두 벨트 위치를 보간(Lerp)
                        if (beltIndex < seg.Belts.Count - 1 && seg.Belts[beltIndex + 1] != null)
                        {
                            itemWorldPos = Vector3.Lerp(currentBelt.transform.position, seg.Belts[beltIndex + 1].transform.position, t);
                        }
                        else
                        {
                            // 세그먼트의 마지막 벨트인 경우 앞방향 벡터를 기준으로 오프셋 계산
                            itemWorldPos = currentBelt.transform.position + currentBelt.transform.forward * (t - 0.5f);
                        }

                        // 벨트 표면 위로 기즈모 위치 보정
                        itemWorldPos.y += 0.6f;
                        Gizmos.DrawSphere(itemWorldPos, 0.15f);
                    }
                }
            }
        }
    }
}
