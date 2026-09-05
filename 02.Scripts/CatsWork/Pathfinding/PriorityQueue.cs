using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatsWork
{
    //역순 출력은 IComparable 인터페이스 구현에서 해결하기
    /// <summary>
    /// Push, Pop, Count 메서드 사용
    /// </summary>
    public class PriorityQueue<T> where T : IComparable<T>
    {
        List<T> heap = new();

        public void Push(T data)
        {
            //힙의 맨 끝에 새 데이터 삽입
            heap.Add(data);

            int now = heap.Count - 1;
            //도장깨기
            while(now > 0)
            {
                //부모의 인덱스
                int next = (now - 1) / 2;
                if (heap[now].CompareTo(heap[next]) < 0)
                    break; //실패

                //자식이 부모보다 크므로 두 값을 교체
                T temp= heap[now];
                heap[now] = heap[next];
                heap[next] = temp;

                //검사 위치 이동
                now = next;
            }
        }

        //루트노드를 Pop
        public T Pop()
        {
            T rootValue = heap[0];

            //마지막 데이터를 루트로 이동
            int lastIndex = heap.Count - 1;
            heap[0] = heap[lastIndex];
            heap.RemoveAt(lastIndex);
            lastIndex--;

            //역으로 내려가는 도장깨기 시작
            int now = 0;
            while(true)
            {
                int left = 2 * now + 1;
                int right = 2 * now + 2;

                int next = now;
                //(범위 내에서)왼쪽값이 현재값보다 크면, 왼쪽으로 이동
                if (left <= lastIndex && heap[next].CompareTo(heap[left]) < 0)
                    next = left;
                //(범위 내에서)오른쪽값이 현재값보다 크면, 오른쪽으로 이동
                if (right <= lastIndex && heap[next].CompareTo(heap[right]) < 0)
                    next = right;

                // 왼쪽, 오른쪽 모두 현재값보다 작으면 종료
                if (next == now)
                    break;


                //두 값을 교체
                T temp = heap[now];
                heap[now] = heap[next];
                heap[next] = temp;

                //검사 위치 이동
                now = next;
            }


            return rootValue;
        }

        public int Count { get { return heap.Count; }}

    }
}