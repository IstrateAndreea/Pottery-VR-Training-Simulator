using UnityEngine;
using System.Collections.Generic;

public class ClayManager : MonoBehaviour
{
    public List<GameObject> clayObjects; 
    private int currentClayIndex = -1;

   
    private PullDeform[] pullScripts;
    private PushDeform[] pushScripts;
    private HoleDeform[] holeScripts;
    private FlattenDeform[] flattenScripts;
    

    void Awake()
    {
        int count = clayObjects.Count;
        pullScripts = new PullDeform[count];
        pushScripts = new PushDeform[count];
        holeScripts = new HoleDeform[count];
        flattenScripts = new FlattenDeform[count];
        

        for (int i = 0; i < count; i++)
        {
            pullScripts[i] = clayObjects[i].GetComponent<PullDeform>();
            pushScripts[i] = clayObjects[i].GetComponent<PushDeform>();
            holeScripts[i] = clayObjects[i].GetComponent<HoleDeform>();
            flattenScripts[i] = clayObjects[i].GetComponent<FlattenDeform>();
           

            clayObjects[i].SetActive(false); 
        }
    }

    public void SelectClay(int index)
    {
        if (index < 0 || index >= clayObjects.Count) return;

        
        for (int i = 0; i < clayObjects.Count; i++)
            clayObjects[i].SetActive(false);

        
        clayObjects[index].SetActive(true);
        currentClayIndex = index;
    }

    public void DisableAllDeformers()
    {
        if (currentClayIndex < 0) return;
        if (pullScripts[currentClayIndex] != null) pullScripts[currentClayIndex].enabled = false;
        if (pushScripts[currentClayIndex] != null) pushScripts[currentClayIndex].enabled = false;
        if (holeScripts[currentClayIndex] != null) holeScripts[currentClayIndex].enabled = false;
        if (flattenScripts[currentClayIndex] != null) flattenScripts[currentClayIndex].enabled = false;
    }

    public void SetMode(string mode)
    {
        if (currentClayIndex < 0) return;

        pullScripts[currentClayIndex].enabled = false;
        pushScripts[currentClayIndex].enabled = false;
        holeScripts[currentClayIndex].enabled = false;
        flattenScripts[currentClayIndex].enabled = false;

        switch (mode)
        {
            case "Pull":
                pullScripts[currentClayIndex].enabled = true;
                pullScripts[currentClayIndex].SyncWithCurrentMesh();
                break;
            case "Push":
                pushScripts[currentClayIndex].enabled = true;
                pushScripts[currentClayIndex].SyncWithCurrentMesh();
                break;
            case "Hole":
                holeScripts[currentClayIndex].enabled = true;
                holeScripts[currentClayIndex].SyncWithCurrentMesh();
                break;
            case "Flatten":
                flattenScripts[currentClayIndex].enabled = true;
                flattenScripts[currentClayIndex].SyncWithCurrentMesh();
                break;
        }
    }

    


}