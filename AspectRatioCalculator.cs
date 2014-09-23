using UnityEngine;
using System.Collections;

public class AspectRatioCalculator : MonoBehaviour
{
  #region public variables
  public Vector2 NativeScreenDimensions;
  public static Vector2 GetNativeDimensions { get { return _nativeScreenDimensions; } }
  #endregion

  #region private variables
  private static Vector2 _nativeScreenDimensions,
                         _scale;
  private static Matrix4x4 _saveMatrix;
  #endregion

  // initializes the native screen size
  void Awake()
  {
    _nativeScreenDimensions = NativeScreenDimensions;
  }

  // gets the scale needed for the gui based on the current screen size
  public static Vector3 GetGUIScaleValue()
  {
    return new Vector3(Screen.width / _nativeScreenDimensions.x, 
                       Screen.width / _nativeScreenDimensions.x, 1);
  }

  //sets the TRS matrix with the appropriate scale and position offset
  public static void SetMatrix(float y, Vector3 scale)
  {
    _saveMatrix = GUI.matrix;

    var yOffset = (y - (y * scale.y)) + (Screen.height - _nativeScreenDimensions.y);
    GUI.matrix = Matrix4x4.TRS(new Vector3(0, yOffset, 0), Quaternion.identity, scale);
  }

  //restores the inital TRS matrix
  public static void ResetMatrix()
  {
    GUI.matrix = _saveMatrix;
  }
}
