using UnityEngine;
using Windows.Kinect;

public class InfraredSourceView : MonoBehaviour
{
    private static InfraredSourceView _instance;
    private KinectSensor _Sensor;

    private InfraredFrameReader _Reader;
    public ushort[] _Data;

    private byte[] _RawData;
    private Texture2D _Texture;
    public static InfraredSourceView Instance { get { return _instance; } }

    public Texture2D GetInfraredTexture()
    {
        return _Texture;
    }

    private void Start()
    {
        _instance = this;

        _Sensor = KinectSensor.GetDefault();

        if (_Sensor != null)
        {
            _Reader = _Sensor.InfraredFrameSource.OpenReader();
            var frameDesc = _Sensor.InfraredFrameSource.FrameDescription;
            _Data = new ushort[frameDesc.LengthInPixels];
            _RawData = new byte[frameDesc.LengthInPixels * 4];
            _Texture = new Texture2D(frameDesc.Width, frameDesc.Height, TextureFormat.BGRA32, false);

            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }
        }

        gameObject.GetComponent<Renderer>().material.SetTextureScale("_MainTex", new Vector2(-1, 1));
    }

    public void View(ushort[] data)
    {
        if (data != null)
        {
            int index = 0;
            foreach (var ir in data)
            {
                byte intensity = (byte)(ir >> 8);
                _RawData[index++] = intensity;
                _RawData[index++] = intensity;
                _RawData[index++] = intensity;
                _RawData[index++] = 255; // Alpha
            }

            _Texture.LoadRawTextureData(_RawData);
            _Texture.Apply();
        }

        gameObject.GetComponent<Renderer>().material.mainTexture = GetInfraredTexture();
    }

    private void Update()
    {
        #region 调用kinect 查看红外图

        /*
           if (_Reader != null)
           {
               var frame = _Reader.AcquireLatestFrame();
               if (frame != null)
               {
                   frame.CopyFrameDataToArray(_Data);

                   int index = 0;
                   foreach (var ir in _Data)
                   {
                       byte intensity = (byte)(ir >> 8);
                       _RawData[index++] = intensity;
                       _RawData[index++] = intensity;
                       _RawData[index++] = intensity;
                       _RawData[index++] = 255; // Alpha
                   }

                   _Texture.LoadRawTextureData(_RawData);
                   _Texture.Apply();

                   frame.Dispose();
                   frame = null;
               }
           }

       */

        #endregion 调用kinect 查看红外图

        if (_Data != null)
        {
            int index = 0;
            foreach (var ir in _Data)
            {
                byte intensity = (byte)(ir >> 8);
                _RawData[index++] = intensity;
                _RawData[index++] = intensity;
                _RawData[index++] = intensity;
                _RawData[index++] = 255; // Alpha
            }

            _Texture.LoadRawTextureData(_RawData);
            _Texture.Apply();
        }

        gameObject.GetComponent<Renderer>().material.mainTexture = GetInfraredTexture();
    }
}