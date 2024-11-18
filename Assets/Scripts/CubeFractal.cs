using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CubeFractal : MonoBehaviour
{
    public int maxDepth = 3;
    public float initialSize = 1f;
    public float scaleFactor = 0.5f;
    public Material cubeMaterial;
    public Color startColor = Color.white;
    public Color endColor = Color.blue;

    public float createDelay = 0.1f;  // 큐브 생성 간격
    public float destroyDelay = 0.5f;  // 큐브 제거 간격
    public float cycleDelay = 2f;     // 완성된 후 대기 시간
    public float fadeOutDuration = 1.0f;  // 페이드 아웃 지속 시간
    public float rotationSpeed = 30f;  // 회전 속도 (도/초)

    private List<GameObject> allCubes = new List<GameObject>();  // 모든 큐브 추적
    private GameObject fractalParent;  // 모든 큐브의 부모 객체
    private Dictionary<GameObject, int> cubeDepths = new Dictionary<GameObject, int>();  // 각 큐브의 depth 저장

    void Start()
    {
        CreateFractalParent();
        StartCoroutine(FractalCycle());
    }

    void Update()
    {
        if (fractalParent != null)
        {
            // 전체 프랙탈을 Y축을 중심으로 회전
            fractalParent.transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
        }
    }

    void CreateFractalParent()
    {
        fractalParent = new GameObject("FractalParent");
        fractalParent.transform.position = transform.position;
    }

    IEnumerator FractalCycle()
    {
        while (true)  // 무한 반복
        {
            // 프랙탈 생성
            yield return StartCoroutine(CreateFractal(transform.position, initialSize, 0));

            // 완성된 상태로 대기
            yield return new WaitForSeconds(cycleDelay);

            // 프랙탈 제거
            yield return StartCoroutine(DestroyFractal());

            // 다음 사이클 전 잠시 대기
            yield return new WaitForSeconds(1f);
        }
    }

    IEnumerator CreateFractal(Vector3 position, float size, int depth)
    {
        // 큐브 생성
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        allCubes.Add(cube);  // 리스트에 추가
        cubeDepths[cube] = depth;  // depth 저장
        cube.transform.position = position;
        cube.transform.localScale = Vector3.one * size;

        // 큐브를 fractalParent의 자식으로 설정
        cube.transform.SetParent(fractalParent.transform);

        // 색상 설정
        Renderer renderer = cube.GetComponent<Renderer>();
        if (cubeMaterial != null)
        {
            Material newMaterial = new Material(cubeMaterial);
            float colorLerp = (float)depth / maxDepth;
            newMaterial.color = Color.Lerp(startColor, endColor, colorLerp);
            renderer.material = newMaterial;
        }

        yield return new WaitForSeconds(createDelay);

        if (depth >= maxDepth) yield break;

        float newSize = size * scaleFactor;
        float offset = (size + newSize) * 0.5f;

        // 각 면에 새로운 큐브 생성
        yield return StartCoroutine(CreateCubesOnFaces(position, newSize, depth, offset));
    }

    IEnumerator CreateCubesOnFaces(Vector3 position, float size, int depth, float offset)
    {
        bool[] occupiedDirections = new bool[6];
        Collider[] colliders;
        float checkDistance = size * 0.6f;

        Vector3[] directions = {
            Vector3.up, Vector3.down,
            Vector3.right, Vector3.left,
            Vector3.forward, Vector3.back
        };

        // 먼저 모든 방향 체크
        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 checkPosition = position + directions[i] * offset;
            // OverlapSphere 대신 OverlapBox 사용
            colliders = Physics.OverlapBox(
                checkPosition,
                Vector3.one * (size * 0.4f)  // 박스 크기를 큐브 크기의 40%로 설정
            );
            occupiedDirections[i] = colliders.Length > 0;
        }

        // 모든 가능한 방향에 대해 동시에 코루틴 시작
        List<Coroutine> coroutines = new List<Coroutine>();

        for (int i = 0; i < directions.Length; i++)
        {
            if (!occupiedDirections[i])
            {
                Vector3 newPos = position + directions[i] * offset;
                coroutines.Add(StartCoroutine(CreateFractal(newPos, size, depth + 1)));
            }
        }

        // 모든 코루틴이 완료될 때까지 대기
        foreach (var coroutine in coroutines)
        {
            yield return coroutine;
        }
    }

    IEnumerator DestroyFractal()
    {
        Dictionary<int, List<GameObject>> cubesByDepth = new Dictionary<int, List<GameObject>>();

        foreach (GameObject cube in allCubes)
        {
            if (cube != null)
            {
                int depth = cubeDepths[cube];  // 저장된 depth 사용
                if (!cubesByDepth.ContainsKey(depth))
                {
                    cubesByDepth[depth] = new List<GameObject>();
                }
                cubesByDepth[depth].Add(cube);
            }
        }

        // 가장 깊은 레벨부터 시작하여 각 레벨의 큐브들을 동시에 제거
        for (int depth = maxDepth; depth >= 0; depth--)
        {
            if (cubesByDepth.ContainsKey(depth))
            {
                foreach (GameObject cube in cubesByDepth[depth])
                {
                    if (cube != null)
                    {
                        Destroy(cube);
                    }
                }
                yield return new WaitForSeconds(destroyDelay);
            }
        }

        // 모든 큐브가 제거된 후 부모 객체도 제거
        Destroy(fractalParent);
        CreateFractalParent();  // 다음 사이클을 위해 새로운 부모 생성

        cubeDepths.Clear();  // Dictionary 초기화
        allCubes.Clear();
    }
}