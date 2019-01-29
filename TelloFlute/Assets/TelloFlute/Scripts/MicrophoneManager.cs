using System.Linq;
using UnityEngine;

namespace TelloFlute
{
    /// <summary>
    /// マイク入力の管理クラス
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MicrophoneManager : MonoBehaviour
    {
        /// <summary>
        /// TELLOコントローラ
        /// </summary>
        [SerializeField]
        private TelloController controller;

        /// <summary>
        /// 周波数を表示するラベル
        /// </summary>
        public TextMesh freqLabel;

        /// <summary>
        /// 周波数をビジュアル表示するためのオブジェクト
        /// </summary>
        public GameObject cube;

        /// <summary>
        /// 接続されているマイクのリスト
        /// </summary>
        private string[] inputDevices;

        /// <summary>
        /// 現在のマイク
        /// </summary>
        private string currentAudioInput = "none";

        /// <summary>
        /// マイクが接続されているかどうかのフラグ
        /// </summary>
        private bool micConnected = false;

        /// <summary>
        /// 遅延間隔の調整用
        /// </summary>
        private const float delay = 0.030f;

        /// <summary>
        /// サンプリングレート
        /// </summary>
        private float samplingRate;

        /// <summary>
        /// 音源
        /// </summary>
        private AudioSource audioSrc;

        /// <summary>
        /// 分解能
        /// </summary>
        private const int resolution = 1024;

        /// <summary>
        /// 周波数ごとの強さを格納する配列
        /// </summary>
        private float[] spectrum = new float[resolution];

        /// <summary>
        /// 検出する周波数の閾値
        /// 音声に過敏に反応しすぎたり、逆に反応しにくい場合に調整する
        /// </summary>
        private const float threshold = 0.005f;

        /// <summary>
        /// 周波数をビジュアル表示するためのオブジェクト
        /// </summary>
        private GameObject[] lines;

        /// <summary>
        /// 音階文字列を格納するバッファのサイズ
        /// </summary>
        private const int maxStrCount = 3;

        /// <summary>
        /// 音階文字列を格納するバッファ
        /// </summary>
        private string[] strBuffer = new string[maxStrCount];

        /// <summary>
        /// 現在のバッファの位置
        /// </summary>
        private int strIndex = 0;

        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Start()
        {
            InitDrawObj();
            InitMicrophone();
        }

        /// <summary>
        /// 周波数をビジュアル表示するためのオブジェクトの初期化
        /// </summary>
        private void InitDrawObj()
        {
            // cube生成(ライン描画)
            lines = new GameObject[resolution];
            for (int i = 0; i < resolution; i++)
            {
                Vector3 pos = new Vector3(0.0005f * i - 0.0005f * resolution / 2, 0f, 2f);
                var obj = Instantiate(cube, pos, Quaternion.identity) as GameObject;
                lines[i] = obj;
            }
        }

        /// <summary>
        /// マイク入力による録音の初期化処理
        /// </summary>
        private void InitMicrophone()
        {
            // AudioSourceの参照取得
            audioSrc = GetComponent<AudioSource>();

            // マイクが接続されていない場合は何もしない
            if (Microphone.devices.Length <= 0)
            {
                Debug.LogWarning("Microphone not connected!");
                return;
            }

            // 名前により識別された、利用可能なマイクデバイスの一覧を取得
            inputDevices = new string[Microphone.devices.Length];
            for (int i = 0; i < Microphone.devices.Length; i++)
            {
                inputDevices[i] = Microphone.devices[i].ToString();
                Debug.Log("Device: " + i + ": " + inputDevices[i]);
            }

            // 0番目のマイクを利用
            currentAudioInput = Microphone.devices[0].ToString();

            // マイクが接続されているフラグを設定
            micConnected = true;

            // マイクから拾った音を再生しないためにはAudioMixerにより音を塞ぐ必要がある
            // 以下のmuteでは音が拾えなくなるためNG
            //audioSrc.mute = true;

            int minFreq;
            int maxFreq;
            // デフォルトマイクの周波数の範囲を取得する
            Microphone.GetDeviceCaps(null, out minFreq, out maxFreq);
            //minFreq や maxFreq 引数で 0 の値が返されるとデバイスが任意の周波数をサポートすることを示す
            if (minFreq == 0 && maxFreq == 0)
            {
                // 録音サンプリングレートを48000 Hzに設定する
                maxFreq = 48000;
            }
            samplingRate = maxFreq;
            /*
            Debug.Log("minFreq:" + minFreq);
            Debug.Log("maxFreq:" + maxFreq);
            Debug.Log("outputSampleRate:" + AudioSettings.outputSampleRate);
            */

            // 録音中でなければ処理を行う
            if (!Microphone.IsRecording(null))
            {
                // 録音開始
                audioSrc.clip = Microphone.Start(currentAudioInput, true, 5, (int)samplingRate);

                // マイクが取れるまで待つ
                while (Microphone.GetPosition(currentAudioInput) <= 0) { }

                // 再生開始
                audioSrc.Play();
            }
        }

        /// <summary>
        ///  毎フレーム行う処理
        /// </summary>
        private void Update()
        {
            if (!micConnected)
            {
                // マイクが接続されていない場合はなにもしない
                return;
            }

            // delayによりレスポンスを調整する
            int microphoneSamples = Microphone.GetPosition(currentAudioInput);
            if (microphoneSamples / samplingRate > delay)
            {
                audioSrc.timeSamples = (int)(microphoneSamples - (delay * samplingRate));
                audioSrc.Play();
            }

            var strongestFrequency = DetectStrongestFrequency();
            string scale = ConvertFreqToScale(strongestFrequency);

            // ラベル更新
            freqLabel.text = strongestFrequency.ToString("0.0") + "(" + scale + ")";

            // 音階をバッファにためる(過敏な反応を抑えるための処理)
            strBuffer[strIndex] = scale;
            strIndex++;
            if (strIndex >= maxStrCount)
            {
                strIndex = 0;
            }

            // TELLOに命令を送信
            SendCommand();

            // 周波数のビジュアル化
            for (int i = 0; i < resolution; i++)
            {
                Vector3 s = lines[i].transform.localScale;
                s.y = spectrum[i] * 200;
                lines[i].transform.localScale = s;
            }
        }

        /// <summary>
        /// 最も強い周波数を検出する
        /// </summary>
        /// <returns>最も強い周波数</returns>
        private float DetectStrongestFrequency()
        {
            audioSrc.GetSpectrumData(spectrum, 0, FFTWindow.Hamming);
            float maxValue = 0;
            int maxIndex = 0;

            for (int i = 0; i < resolution; i++)
            {
                if (spectrum[i] > maxValue && spectrum[i] > threshold)
                {
                    maxValue = spectrum[i];
                    maxIndex = i;
                }
            }

            float freq = maxIndex * (AudioSettings.outputSampleRate / 2) / resolution;
            return freq;
        }

        /// <summary>
        /// 周波数を音階へ変換する
        /// </summary>
        /// <param name="freq">周波数</param>
        /// <returns>オクターブを表す数値を含む音階文字列</returns>
        private string ConvertFreqToScale(float freq)
        {
            // 27.5Hzのラの音を基準にする
            float A0 = 27.500f;
            // 音階テーブル
            string[] scales = new string[] { "A", "A#", "B", "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#" };

            if (freq <= 0.0f)
            {
                return "";
            }
            else
            {
                // 音階
                float value = 12.0f * Mathf.Log(freq / A0) / Mathf.Log(2.0f);
                string scale = scales[(int)(Mathf.Round(value) % 12)];

                // オクターブを表す数値
                int octave = (int)((int)(Mathf.Round(value) / 12));

                return scale + octave.ToString();
            }
        }

        /// <summary>
        /// 音階に応じた命令をTELLOに送信する
        /// </summary>
        private void SendCommand()
        {
            if (DetectKey(strBuffer, "C4") || DetectKey(strBuffer, "C#4"))
            {
                controller.SendCommand("takeoff");
            }
            else if (DetectKey(strBuffer, "D4"))
            {
                controller.SendCommand("forward 50");
            }
            else if (DetectKey(strBuffer, "E4"))
            {
                controller.SendCommand("right 50");
            }
            else if (DetectKey(strBuffer, "F4"))
            {
                controller.SendCommand("back 50");
            }
            else if (DetectKey(strBuffer, "G4"))
            {
                controller.SendCommand("left 50");
            }
            else if (DetectKey(strBuffer, "A5"))
            {
                controller.SendCommand("flip l");
            }
            else if (DetectKey(strBuffer, "B5"))
            {
                controller.SendCommand("flip r");
            }
            else if (DetectKey(strBuffer, "C5"))
            {
                controller.SendCommand("land");
            }
        }

        /// <summary>
        /// 配列内の値がすべて指定したキー名と同じかどうかを確認する
        /// (一定時間同じ音が続いたかどうかを判定する)
        /// </summary>
        /// <param name="list">チェックする配列</param>
        /// <param name="key">音階名</param>
        /// <returns>すべて同じ音階名のときにtrue、それ以外はfalseを返す<returns>
        private bool DetectKey(string[] list, string key)
        {
            if (list.Distinct().Count() == 1 && list[0] == key)
            {
                return true;
            }
            return false;
        }

    } // class MicrophoneManager
} // namespace TelloFlute