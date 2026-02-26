using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Troop : MonoBehaviour
{
    [Header("Troop Settings")]
    [SerializeField] private int maxCarry = 7;
    public int id;
    
    private int currentCarry = 0;
    public int CurrentCarry
    {
        get => currentCarry;
    }
    public int MaxCarry
    {
        get => maxCarry;
    }

    // i'm aware this makes the FSM a mess but I didn't want to refactor all of the CharacterTasks to work
    public bool gathering = false;
    public bool attacking = false;
    public bool returning = false;
    public bool death = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
    }

    public IEnumerator GatherResources(ResourceSite resourceSite)
    {
        while (currentCarry < maxCarry)
        {
            currentCarry++;
            yield return new WaitForSeconds(6.0f / resourceSite.ResourceRate);
        }

        gathering = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        GameObject go = other.gameObject;
        if (go.CompareTag("ResourceSite") && !gathering)
        {
            gathering = true;
            StartCoroutine(GatherResources(other.GetComponent<ResourceSite>()));
        }
        if (go.CompareTag("Troop"))
        {
            Troop trp = go.GetComponent<Troop>();

            // you can't kill your own troops
            if (trp.id == id) return;

            if (attacking)
            {
                go.GetComponent<Troop>().death = true;
                death = true;
            }
            else
            {
                death = true;
            }
        }
        if (go.CompareTag("Base") && returning)
        {
            go.GetComponent<Base>().DepositResources(currentCarry);
            currentCarry = 0;
            returning = false;
        }
    }
}
