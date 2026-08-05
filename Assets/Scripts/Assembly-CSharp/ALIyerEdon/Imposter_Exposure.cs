using UnityEngine;

namespace ALIyerEdon
{
	[ExecuteInEditMode]
	public class Imposter_Exposure : MonoBehaviour
	{
		public float exposure = 1f;

		public Color color;

		public bool apply;

		private void Start()
		{
			Apply_Imposter_Exposure(exposure);
		}

		private void Update()
		{
			if (apply)
			{
				Apply_Imposter_Exposure(exposure);
				apply = false;
			}
		}

		public void Apply_Imposter_Exposure(float exposureValue)
		{
			Shader.SetGlobalFloat("ImposterExposure", exposureValue);
			Shader.SetGlobalColor("ImposterColor", color);
		}
	}
}
