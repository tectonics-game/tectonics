using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AssemblyCSharp
{
	public class tectonicPixel
	{
		Vector2 position;
		float height;
		tectonicPlate plate;

		public tectonicPixel (Vector2 myposition)
		{
			position = myposition;
		}

		public void setPlate(tectonicPlate myPlate){
			plate = myPlate;
		}
	}
}

