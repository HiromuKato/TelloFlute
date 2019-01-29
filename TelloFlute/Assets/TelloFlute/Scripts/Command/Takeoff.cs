using UnityEngine;
using HoloToolkit.Unity.InputModule;

namespace TelloFlute
{
    public class Takeoff : MonoBehaviour, IInputClickHandler
    {
        [SerializeField]
        private TelloController client;

        /// <summary>
        /// タップされた時の処理を行います
        /// </summary>
        /// <param name="eventData">イベントデータ</param>
        public void OnInputClicked(InputClickedEventData eventData)
        {
            // TELLOにtakeoff命令を送信する
            client.SendCommand("takeoff");
        }

    } // class Takeoff
} // namespace TelloFlute
