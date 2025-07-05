namespace Quantum.Asteroids
{
  using Photon.Deterministic;
  using UnityEngine;

  /// <summary>
  /// The <c>AsteroidsQuantumInput</c> class handles the input for the Asteroids game
  /// by subscribing to Quantum's input callback system.
  /// </summary>
  public class AsteroidsQuantumInput : MonoBehaviour
  {
    /// <summary>
    /// Subscribes to the Quantum input callback when the script is enabled.
    /// </summary>
    private void OnEnable()
    {
      QuantumCallback.Subscribe(this, (CallbackPollInput callback) => PollInput(callback));
    }

    /// <summary>
    /// Polls the current input state and sets it in the Quantum input callback.
    /// </summary>
    /// <param name="callback">The input callback provided by Quantum.</param>
    public void PollInput(CallbackPollInput callback)
    {
      Quantum.Input i = new Quantum.Input();
      i.Left = UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow);
      i.Right = UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow);
      i.Up = UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow);
      i.Fire = UnityEngine.Input.GetKey(KeyCode.Space);
      callback.SetInput(i, DeterministicInputFlags.Repeatable);
    }
  }
}