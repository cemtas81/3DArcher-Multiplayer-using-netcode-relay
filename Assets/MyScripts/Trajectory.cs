using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trajectory : MonoBehaviour
{
    [SerializeField] int dotsNumber;
    [SerializeField] GameObject DotsParent;
    [SerializeField] GameObject DotsPerfab;
    [SerializeField] float dotSpacing;
    [SerializeField] [Range(0.01f, 0.1f)] float dotMinScale;
    [SerializeField] [Range(0.1f, 0.3f)] float dotMaxScale;

    Transform[] dotsList;
    Vector3 pos;
    float TimeStamp;

    private void Start()
    {
        Hide();
        PrepareDots();
    }
    public void Show()
    {
        DotsParent.SetActive(true);
    }
    public void Hide()
    {
        DotsParent.SetActive(false);
    }

    void PrepareDots()
    {
        dotsList = new Transform[dotsNumber];
        DotsPerfab.transform.localScale = Vector3.one * dotMaxScale;

        float scale = dotMaxScale;
        float scalefactor = scale / dotsNumber;

        for(int i = 0; i < dotsNumber; i++)
        {
            dotsList[i] = Instantiate(DotsPerfab, null).transform;
            dotsList[i].parent = DotsParent.transform;

            dotsList[i].localScale = Vector3.one * scale;
            if(scale > dotMinScale)
            {
                scale -= scalefactor;
            }
        }
    }

    public void UpdateDots(Vector3 ballPos , Vector3 forceApplied)
    {
        TimeStamp = dotSpacing;
        for(int i = 0; i < dotsNumber; i++)
        {
            pos.x = (ballPos.x + forceApplied.x * TimeStamp);
            pos.z = (ballPos.z + forceApplied.z * TimeStamp);
            pos.y = (ballPos.y + forceApplied.y * TimeStamp) - (Physics.gravity.magnitude * TimeStamp * TimeStamp) / 2f;

            dotsList[i].position = pos;
            TimeStamp += dotSpacing;
        }
    }
}
