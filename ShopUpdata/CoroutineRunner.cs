using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AutoBuy
{
    public class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner instance;

        public static CoroutineRunner Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject obj = new GameObject("CoroutineRunner");
                    instance = obj.AddComponent<CoroutineRunner>();
                    DontDestroyOnLoad(obj); // 可选：跨场景保留
                }
                return instance;
            }
        }

        // 提供静态接口来启动协程
        public static Coroutine StartStaticCoroutine(IEnumerator routine)
        {
            return Instance.StartCoroutine(routine);
        }

        // 可选：提供停止协程的方法
        public static void StopStaticCoroutine(Coroutine coroutine)
        {
            if (instance != null && coroutine != null)
                Instance.StopCoroutine(coroutine);
        }
    }
}
