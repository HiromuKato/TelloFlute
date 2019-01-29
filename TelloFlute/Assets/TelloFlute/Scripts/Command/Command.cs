using UnityEngine;
using HoloToolkit.Unity.InputModule;

namespace TelloFlute
{
    public class Command : MonoBehaviour, IInputClickHandler
    {
        [SerializeField]
        private TelloController client;

        /// <summary>
        /// タップされた時の処理を行います
        /// </summary>
        /// <param name="eventData">イベントデータ</param
        public void OnInputClicked(InputClickedEventData eventData)
        {
            // TELLOにcommand命令を送信する
            client.SendCommand("command");
        }

    } // class Command
} // namespace TelloFlute