using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Graphics;
using Vector4 = Microsoft.Xna.Framework.Vector4;
using Kinect_Angle_XNA.Util.Extensions;

namespace Kinect_Angle_XNA
{
  class KinectManager
  {
    #region public variables
    public delegate void AssignSkeletonInfoEvent(SkeletonInfo info, int playerIter);
    public delegate void RemoveSkeletonInfoEvent(int playerIter);
    public static event RemoveSkeletonInfoEvent RemoveSkeletonInfo;
    public static event AssignSkeletonInfoEvent AssignSkeletonInfo;
    public static event QuitEvent Quit;
    public bool IsConnected { get { return _sensor != null; } }
    #endregion

    #region private variables
    private KinectSensor _sensor = null;
    private SkeletonFrame _frame = null;
    private Skeleton[] _skeletons = null;
    private static List<Skeleton> _skeletonList;
    private CoordinateMapper _coordMapper;
    private string _message;
    private int _lastCount;
    private Entity _cursor;
    private Color _clearColor;
    #endregion

    //Sets up the Kinect for skeleton data collection
    public KinectManager()
    {
      _sensor = KinectSensor.KinectSensors.FirstOrDefault(s => s.Status == KinectStatus.Connected);

      _tryConnect();

      if (_sensor != null)
      {
        _coordMapper = new CoordinateMapper(_sensor);

        _skeletons = new Skeleton[_sensor.SkeletonStream.FrameSkeletonArrayLength];
        _sensor.SkeletonFrameReady += GetNextSkeletonFrame;
        _sensor.SkeletonStream.Enable();
        _sensor.Start();

        _skeletonList = new List<Skeleton>();

        _lastCount = 0;
        _loadCursor();
      }

      _message = ",,,,,,\n";
      _clearColor = new Color(new Vector4(0, 0, 0, 0));
    }
    
    // Update each skeleton frame and populate skeleton info.
    public void Update(GameState state)
    {
      if ((_sensor == null || _sensor.Status == KinectStatus.Disconnected))
      {
        _sensor = null;
        _tryConnect();
      }

      if (_skeletons == null)
        return;

      if (_lastCount != _skeletonList.Count || _skeletonList.Count == 0 
          || _skeletonList.Count < IPlayerBehavior.NumPlayers)
      {
        _getTrackedSkeletons();
      }

      for (int i = 0; i < _skeletonList.Count; i++)
      {
        if (_skeletonList[i].TrackingState == SkeletonTrackingState.Tracked)
        {
          _populateSkelInfo(i, state);
        }
        else
        {
          RemoveSkeletonInfo(i);
          _skeletonList = _skeletonList.Where(x => x.TrackingState == SkeletonTrackingState.Tracked).ToList();
          break;
        }
      }
    }

    //pulls the next skeleton frame 
    public void GetNextSkeletonFrame(object sender, SkeletonFrameReadyEventArgs e)
    {
      _frame = e.OpenSkeletonFrame();

      if (_frame != null)
      {
        _frame.CopySkeletonDataTo(_skeletons);
        _frame.Dispose();
      }
    }

    //cleans up the sensor
    public void CleanUp()
    {
      if (_sensor == null)
        return;

      _sensor.Stop();  
      _sensor.Dispose();
    }

    //fills the skeleton info with infromation from the last skeleton frame
    private void _populateSkelInfo(int iter, GameState state)
    {
      SkeletonInfo tmp = new SkeletonInfo();
      tmp.TrackingID = _skeletonList[iter].TrackingId;
      tmp.Tracked = _skeletonList[iter].TrackingState;

      var rightHand = _skeletonList[iter].Joints[JointType.HandRight];

      tmp.DisplayPosition = _coordMapper.MapSkeletonPointToColorPoint(_skeletonList[iter].Joints[JointType.ShoulderCenter].Position, ColorImageFormat.RgbResolution640x480Fps30);
      tmp.LeftHand = _coordMapper.MapSkeletonPointToColorPoint(_skeletonList[iter].Joints[JointType.HandLeft].Position, ColorImageFormat.RgbResolution640x480Fps30);
      tmp.RightHand = _coordMapper.MapSkeletonPointToColorPoint(rightHand.Position, ColorImageFormat.RgbResolution640x480Fps30);
      tmp.RightShoulder = _coordMapper.MapSkeletonPointToColorPoint(_skeletonList[iter].Joints[JointType.ShoulderRight].Position, ColorImageFormat.RgbResolution640x480Fps30);

      if ((state ^ GameState.SinglePlayer) == GameState.Running
          || (state ^ GameState.MultiPlayer) == GameState.Running)
      {
        _cursor.ToggleDisplay(false);
        var rightVec = new Vector2();
        var leftVec = new Vector2();

        _calculateVectors(ref rightVec, ref leftVec, tmp);

        _selectNextColor(rightVec, leftVec, ref tmp);
        _getIndividualRot(rightVec, leftVec, ref tmp);
      }
      else if(iter < 1)
      {
        _cursor.ToggleDisplay(true);

        var world = new Vector2(640, 480);
        var width = world.X * .4f;
        var height = world.Y * .4f;
        var bounding = new Rectangle(tmp.RightShoulder.X - (int)(width / 2),
                                     tmp.RightShoulder.Y - (int)(height / 2),
                                     (int)width, (int)height);

        var location = new ColorImagePoint();
        if (bounding.Contains(tmp.RightHand.X, tmp.RightHand.Y))
        {
          location.X = tmp.RightHand.X - bounding.X;
          location.Y = tmp.RightHand.Y - bounding.Y;
          location = location.BoundingToWorldSpace(new Vector2(width, height), world);
        }
        _cursor.Pos = new Vector2(location.X, location.Y);
        _cursor.Update(state);
      }

      AssignSkeletonInfo(tmp, iter);        
    }

    //draws the cursor to the screen
    public RenderTarget2D Draw(GraphicsDevice device, SpriteBatch sb)
    {
      var renderTarget = new RenderTarget2D(device, 640, 480);
      device.SetRenderTarget(renderTarget);
      device.Clear(_clearColor);

      sb.Begin();
      {
        _cursor.Draw(sb);
      }
      sb.End();
      return renderTarget;
    }

    //loads the cursors sprite
    public void Load()
    {
      _cursor.Sprite = SpriteManager.GetSprite(_cursor.SpriteName);
    }

    //Checks to see if the kinect sensor is connected
    private void _tryConnect()
    {
      while (_sensor == null || _sensor.Status != KinectStatus.Connected)
      {
        if (_sensor != null)
          continue;

        if (MessageBox.Show("There are no Kinect Sensors connected.\n Please connect a sensor.", "Connect Kinect Sensor", MessageBoxButtons.OKCancel) == DialogResult.OK)
        {
          _sensor = KinectSensor.KinectSensors.FirstOrDefault();
          continue;
        }
        else
        {
          Quit();
          break;
        }
      }
    }

    //gets the next background color based on the angle between the skeletons arms
    private void _selectNextColor(Vector2 rightVec, Vector2 leftVec, ref SkeletonInfo tmp)
    {
      float angle = (rightVec.X * leftVec.X) + (rightVec.Y * leftVec.Y);
      angle = (float)Math.Acos(angle);
      angle = angle * (180 / (float)Math.PI);

      //var lastScreenColor = tmp.ScreenColor;
      var color = "";

      if (angle >= 87 && angle <= 93)
      {
        tmp.ScreenColor = "Yellow";
        color = "Yellow";
      }
      else if (angle >= 0 && angle <= 87)
      {
        tmp.ScreenColor = "Pink";
        color = "Pink";
      }
      else if (angle > 175 && angle <= 182)
      {
        tmp.ScreenColor = "Violet";
        color = "Dark Blue";
      }
      else if (angle >= 93 && angle <= 175)
      {
        tmp.ScreenColor = "Blue Green";
        color = "Light Blue";
      }

      /*if (tmp.ScreenColor == lastScreenColor)
        _message = "Angle," + color + "," + angle 
                      + "," + DateTime.Today.TimeOfDay.ToString() + ",";
      else
        _message = "Screen Color and Angle," + color + ","
                      + angle + "," + DateTime.Today.TimeOfDay.ToString() + ",";*/
    }

    //gets the individual rotation of each arm
    private void _getIndividualRot(Vector2 rightVec, Vector2 leftVec, ref SkeletonInfo tmp)
    {
      Vector2 zeroVector = new Vector2(1, 0);

      tmp.RightRotation = (float)Math.Acos((rightVec.X * zeroVector.X) + (rightVec.Y * zeroVector.Y));
      tmp.LeftRotation = (float)Math.Acos((leftVec.X * zeroVector.X) + (leftVec.Y * zeroVector.Y));
      
      if (tmp.RightHand.Y < tmp.DisplayPosition.Y)
        tmp.RightRotation *= -1;

      if (tmp.LeftHand.Y < tmp.DisplayPosition.Y)
        tmp.LeftRotation *= -1;

      tmp.RotDiff = tmp.RightRotation - tmp.LeftRotation;

      /*var tmpLeft = tmp.LeftRotation * (180 / Math.PI);
      var tmpRight = tmp.RightRotation * (180 / Math.PI);*/

      //_message += tmpLeft + "," + tmpRight + ","; 
    }

    //Checks to see if there are any tracked skeletons and populates a list with the ones
    //that are tracked
    private void _getTrackedSkeletons()
    {
      var tmpSkel = _skeletons.Where(s => s != null && s.TrackingState == SkeletonTrackingState.Tracked
        && _skeletonList.Where(x => x.TrackingId == s.TrackingId).Count() == 0).ToArray();

      _skeletonList.AddRange(tmpSkel.Where(x => !_skeletonList.Contains(x)));
      _lastCount = _skeletonList.Count;
    }

    //Gets a vector from the players shoulder to their hand
    private void _calculateVectors(ref Vector2 right, ref Vector2 left, SkeletonInfo info)
    {
      right.X = info.RightHand.X - info.DisplayPosition.X;
      right.Y = info.RightHand.Y - info.DisplayPosition.Y;
      right.Normalize();

      left.X = info.LeftHand.X - info.DisplayPosition.X;
      left.Y = info.LeftHand.Y - info.DisplayPosition.Y;
      left.Normalize();
    }

    //loads the cursor image
    private void _loadCursor()
    {
      var file = new StreamReader("Content\\XML\\KinectCursor.xml");
      var reader = new XmlSerializer(typeof(Entity), new Type[] { typeof(Sprite), typeof(Font) });

      _cursor = new Entity();

      try
      {
        _cursor = (Entity)reader.Deserialize(file);
      }
      catch (InvalidOperationException e)
      {
        Console.WriteLine("Error: " + e.Message);

        if (e.InnerException != null)
          Console.WriteLine("Inner Exception: " + e.InnerException);
      }
      file.Close();
    }
  }
}
