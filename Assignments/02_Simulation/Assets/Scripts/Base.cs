using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class Base : MonoBehaviour
{
    [Header("Base Parameters")]
    [SerializeField] private GameObject spawnPrefab;
    [SerializeField] private float baseSpawnRate;
    [SerializeField] private float maxSpawnTimer;
    [SerializeField] private int resources;
    [SerializeField] private int maxResources;
    [SerializeField, Range(0.0f, 1.0f)] private float resourcePriority;

    [Header("Troop Parameters")]
    [SerializeField] private float speed = 0.3f;
    [SerializeField] private float mass = 1;
    [SerializeField] private float maxForce = 1;
    [SerializeField] private float tolerance = 0.1f;
    [SerializeField] private float slowingRadius = 2.5f;

    // internal member instance information
    List<Task> CharacterTasks = new List<Task>(10);
    List<GameObject> Characters = new List<GameObject>(10);
    List<Troop> TroopScripts = new List<Troop>(10);
    List<float> MovementSpeeds = new List<float>(10);
    List<Vector3> Targets = new List<Vector3>(10);
    List<PathMovementInfo> CharacterPathMovementInfo = new List<PathMovementInfo>(10);
    List<List<Vector3>> CharacterPaths = new List<List<Vector3>>(10);
    private List<Transform> CharacterTransforms = new List<Transform>(10);
    private List<Vector3> CharacterVelocities = new List<Vector3>(10);
    private MapData[] MapData = null;
    byte mMinWidth, mMaxWidth;
    byte mMinDepth, mMaxDepth;
    AStarSystem mAStarSystem;
    [SerializeField] private GameManager gameManager;

    // properties
    public List<Transform> _CharacterTransforms => CharacterTransforms;

    // spawner information
    private float spawnTimer;
    private float nextSpawnTime;
    private int spawnCost = 3;

    // Start-initialized variables
    private List<Base> enemyBases;
    private ResourceSite[] resourceSites;

    [SerializeField] private TextMeshProUGUI resourceText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject[] baseGos = GameObject.FindGameObjectsWithTag("Base");
        enemyBases = new List<Base>();
        foreach (GameObject baseGo in baseGos)
        {
            if (baseGo != gameObject)
            {
                enemyBases.Add(baseGo.GetComponent<Base>());
            }
        }

        resourceSites = FindObjectsByType<ResourceSite>(FindObjectsSortMode.None);

        mMinWidth = 0;
        mMinDepth = 0;
        mMaxWidth = gameManager._Width;
        mMaxDepth = gameManager._Depth;
        mAStarSystem = new AStarSystem(mMaxWidth);

        MapData = gameManager.MMap;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateTimer();

        UpdateTroops();

        // update text
        resourceText.text = $"{resources}";
    }

    public void FixedUpdate()
    {
        int count = Characters.Count;
        for (int i = 0; i < count; i++)
        {
            if (CharacterTasks[i] == Task.Move || CharacterTasks[i] == Task.Attack)
            {
                MovementSystem(i);
            }
        }
    }

    private void UpdateTroops()
    {
        // NOTE: if too many chars this would need to be split and
        // time shared
        int count = Characters.Count;
        for (int i = 0; i < count; i++)
        {
            // handle death in any circumstance
            if (TroopScripts[i].death == true)
            {
                CharacterTasks[i] = Task.Death;
            }

            switch (CharacterTasks[i])
            {
                case Task.Idle:
                    if (TroopScripts[i].gathering == true)
                    {
                        CharacterTasks[i] = Task.Gather;
                    }

                    // evaluate closest and pick new target
                    SelectTarget(i);

                    // add the pathing to the aStar queue
                    //Vector3 currentPos = Characters[i].gameObject.GetComponent<Transform>().position;
                    Vector3 currentPos = CharacterTransforms[i].position;
                    mAStarSystem.queueForPathing.Enqueue(
                        new CharacterPathData { characterID = i, target = Targets[i], pos = currentPos });
                    break;
                case Task.Move:
                    if (TroopScripts[i].gathering == true)
                    {
                        CharacterTasks[i] = Task.Gather;
                    }
                    // movement system is called in fixedupdate
                    break;
                case Task.Gather:
                    if (TroopScripts[i].gathering == false)
                    {
                        CharacterTasks[i] = Task.Returning;
                    }
                    break;
                case Task.Attack:
                    MovementSpeeds[i] = speed * 1.5f;
                    if (TroopScripts[i].attacking == false)
                    {
                        MovementSpeeds[i] = speed;
                        CharacterTasks[i] = Task.Idle;
                    }   
                    break;
                case Task.Returning:
                    Targets[i] = transform.position;

                    // add the pathing to the aStar queue
                    Vector3 pos = CharacterTransforms[i].position;
                    mAStarSystem.queueForPathing.Enqueue(
                        new CharacterPathData { characterID = i, target = Targets[i], pos = pos });

                    TroopScripts[i].returning = true;
                    break;
                case Task.Death:
                    // remove character from all lists and destroy gameobject
                    CharacterTasks.RemoveAt(i);
                    TroopScripts.RemoveAt(i);
                    MovementSpeeds.RemoveAt(i);
                    Targets.RemoveAt(i);
                    CharacterPathMovementInfo.RemoveAt(i);
                    CharacterPaths.RemoveAt(i);
                    CharacterTransforms.RemoveAt(i);
                    CharacterVelocities.RemoveAt(i);

                    Destroy(Characters[i]);
                    Characters.RemoveAt(i);

                    i--;
                    count--;
                    break;
                default:
                    break;
            }
        }

        // run AStar on anything that needs it
        mAStarSystem.runAStar(CharacterTasks, CharacterPathMovementInfo, MapData, CharacterPaths);
    }

    private void UpdateTimer()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= nextSpawnTime)
        {
            SpawnUnit();
            spawnTimer = 0f;
            float spawnTime = (1 / ((float) resources / maxResources)) * baseSpawnRate;
            nextSpawnTime = spawnTime > maxSpawnTimer ? maxSpawnTimer : spawnTime;
        }
    }

    private void SpawnUnit()
    {
        if (resources >= spawnCost)
        {
            // create prefab at base location and add to characters list
            GameObject tmp = Instantiate(spawnPrefab, transform.position, Quaternion.identity);
            Characters.Add(tmp);
            TroopScripts.Add(tmp.GetComponent<Troop>());
            MovementSpeeds.Add(1.0f);
            CharacterTransforms.Add(tmp.transform);
            CharacterVelocities.Add(Vector3.zero);
            CharacterTasks.Add(Task.Idle);
            Targets.Add(new Vector3());
            CharacterPathMovementInfo.Add(new PathMovementInfo());
            List<Vector3> path = new List<Vector3>(100);
            CharacterPaths.Add(path);
            tmp.SetActive(true);

            // update resources
            resources -= spawnCost;
        }
    }

    private void SelectTarget(int characterID)
    {
        TargetInfo enemyTarget = FindClosestEnemy(characterID);

        // if an enemy is close or we have enough resources, attack, otherwise gather resources
        if (enemyTarget.dist < 0.5f || resources >= maxResources * resourcePriority)
        {

            if (enemyTarget.target != null)
            {
                Targets[characterID] = enemyTarget.target.transform.position;

                if (enemyTarget.dist < slowingRadius)
                {
                    CharacterTasks[characterID] = Task.Attack;
                    TroopScripts[characterID].attacking = true;
                }
            }
        }
        else
        {
            TargetInfo rsTarget = FindClosestRS(characterID);
            Targets[characterID] = rsTarget.target.transform.position;
        }
    }

    private TargetInfo FindClosestEnemy(int characterID)
    {
        Vector3 pos = CharacterTransforms[characterID].position;

        // find closest resource sites
        float closestDist = float.MaxValue;
        Troop target = null;

        foreach (Base b in enemyBases)
        {
            List<Transform> enemyTroops = b._CharacterTransforms;

            foreach (Transform t in enemyTroops)
            {
                float dist = Vector3.Distance(pos, t.position);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    target = t.gameObject.GetComponent<Troop>();
                }
            }
        }

        return new TargetInfo { dist = closestDist, target = target.gameObject };
    }

    private TargetInfo FindClosestRS(int characterID)
    {
        Vector3 pos = CharacterTransforms[characterID].position;

        // find closest resource sites
        float closestDist = float.MaxValue;
        float bestScore = float.MinValue;
        ResourceSite target = null;

        for (int i = 0; i < resourceSites.Length; i++)
        {
            Vector3 rsPos = resourceSites[i].transform.position;

            // use a weighted metric
            float dist = Vector3.Distance(pos, rsPos);
            float score = (resourceSites[i].ResourceRate * 2.0f) - dist;

            if (score > bestScore)
            {
                closestDist = dist;
                bestScore = score;
                target = resourceSites[i];
            }
        }

        return new TargetInfo { dist = closestDist, target = target.gameObject };
    }

    public void DepositResources(int amount)
    {
        if (resources + amount < maxResources)
        {
            resources += amount;
        }
        else
        {
            resources = maxResources;
        }
    }

    public void MovementSystem(int characterID)
    {
        int currentPathIndex = CharacterPathMovementInfo[characterID].currentIndex;
        List<Vector3> path = CharacterPaths[characterID];
        Vector3 pos = CharacterTransforms[characterID].position;
        PathMovementInfo pathMovement = CharacterPathMovementInfo[characterID];

        if (currentPathIndex < CharacterPathMovementInfo[characterID].length)
        {
            Vector3 currentTarget = new Vector3(path[currentPathIndex].x, pos.y, path[currentPathIndex].z);

            // Calculate DX and DZ (y represents up, therefore we won't be using that in this case).
            // we need to follow targets to get to the end of the path
            float dx = currentTarget.x - pos.x;
            float dz = currentTarget.z - pos.z;
            float xDiff = Mathf.Abs(dx);
            float zDiff = Mathf.Abs(dz);

            if (xDiff > tolerance || zDiff > tolerance)
            {

                VehicleData data = new VehicleData
                {
                    pos = pos,
                    mass = mass,
                    max_force = maxForce,
                    max_speed = speed * MovementSpeeds[characterID],
                    velocity = CharacterVelocities[characterID]
                };

                if (currentPathIndex < pathMovement.length - 1)
                {
                    Seek(ref data, currentTarget);
                }
                else
                {
                    Arrive(ref data, currentTarget);
                }

                // transfer the algorithm results to the graphical model
                CharacterVelocities[characterID] = data.velocity;
                CharacterTransforms[characterID].transform.position = data.pos;
                CharacterTransforms[characterID].transform.forward = Vector3.Normalize(data.velocity);
            }
            else
            {
                if (currentPathIndex < pathMovement.length - 1)
                {
                    pathMovement.currentIndex = pathMovement.currentIndex + 1;
                    CharacterPathMovementInfo[characterID] = pathMovement;
                }
                else
                {
                    CharacterVelocities[characterID] = Vector3.zero;
                    CharacterTasks[characterID] = Task.Idle;
                }

            }
        }
    }


    // Arrive algorithm from a combination of Craig Reynold's Steering paper and 
    // https://code.tutsplus.com/understanding-steering-behaviors-flee-and-arrival--gamedev-1303t
    public void Arrive(ref VehicleData data, Vector3 target)
    {
        Vector3 target_offset = target - data.pos;
        float distance = target_offset.magnitude;
        float ramped_speed = data.max_speed * (distance / slowingRadius);
        float clipped_speed = Mathf.Min(ramped_speed, data.max_speed);

        Vector3 desired_velocity;
        if (distance <= 0.0001f)
        {
            desired_velocity = Vector3.zero;
        }
        else
        {
            desired_velocity = (clipped_speed / distance) * target_offset;
        }
        Vector3 steering = desired_velocity - data.velocity;
        steering = truncateLength(steering, data.max_force);
        Vector3 acceleration = steering / data.mass;
        data.velocity = truncateLength(data.velocity + acceleration, data.max_speed);
        data.pos = data.pos + data.velocity;
    }

    // Seek algorithm is from Craig Reynold's Steering paper
    public void Seek(ref VehicleData data, Vector3 target)
    {
        Vector3 desired_velocity = Vector3.Normalize((target - data.pos) * data.max_speed);
        Vector3 steering = desired_velocity - data.velocity;
        steering = truncateLength(steering, data.max_force);
        Vector3 acceleration = steering / data.mass;
        data.velocity = truncateLength(data.velocity + acceleration, data.max_speed);
        data.pos = data.pos + data.velocity;
    }


    // This is a clamping function so that the vector 
    // input does not get larger than the maxLength entered
    public Vector3 truncateLength(Vector3 vector, float maxLength)
    {
        float maxLengthSquared = maxLength * maxLength;
        float vecLengthSquared = vector.sqrMagnitude;
        if (vecLengthSquared <= maxLengthSquared)
            return vector;
        else
            return (vector) * (maxLength / Mathf.Sqrt((vecLengthSquared)));
    }
}
