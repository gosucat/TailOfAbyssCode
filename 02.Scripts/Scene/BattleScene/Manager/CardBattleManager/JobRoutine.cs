using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum JobType
{
    CardFunc,       //카드 기능
    Draw,           //단순 드로우
    SetCardToUsed,  //카드를 사용한 카드 더미로
    Sequence,       //특수 작업 : 현재 카드 선택창 띄우기
}

public class JobRoutine
{
    public JobType JobType;
    public IEnumerator JobCo;

    public JobRoutine(JobType jobType, IEnumerator jobCo)
    {
        JobType = jobType;
        JobCo = jobCo;
    }
}
