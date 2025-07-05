namespace Quantum {
  using Photon.Deterministic;
  using UnityEngine;

  public class QuantumDebugInput : MonoBehaviour {

    private void OnEnable() {
      QuantumCallback.Subscribe(this, (CallbackPollInput callback) => PollInput(callback));
    }

    public void PollInput(CallbackPollInput callback) {
      Quantum.Input input = new Quantum.Input();

      float h = UnityEngine.Input.GetAxisRaw("Horizontal");
      float v = UnityEngine.Input.GetAxisRaw("Vertical");

      Vector3 direction = new Vector3(h, 0, v).normalized;

      if (direction != Vector3.zero) {
        input.Direction = new FPVector3(
          FP.FromFloat_UNSAFE(direction.x),
          FP.FromFloat_UNSAFE(0f),
          FP.FromFloat_UNSAFE(direction.z)
        );
      } else {
        input.Direction = FPVector3.Zero;
      }

      callback.SetInput(input, DeterministicInputFlags.Repeatable);
    }
  }
}
