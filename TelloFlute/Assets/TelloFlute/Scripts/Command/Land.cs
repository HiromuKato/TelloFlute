using UnityEngine;
using HoloToolkit.Unity.InputModule;

namespace TelloFlute
{
    public class Land : MonoBehaviour, IInputClickHandler
    {
        [SerializeField]
        private TelloController client;

        /// <summary>
        /// タップされた時の処理を行います
        /// </summary>
        /// <param name="eventData">イベントデータ</param
        public void OnInputClicked(InputClickedEventData eventData)
        {
            // TELLOにland命令を送信する
            client.SendCommand("land");
        }

    } // class Land
} // namespace TelloFlute
