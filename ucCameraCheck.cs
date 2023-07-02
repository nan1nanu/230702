using PacTech.Inspection;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;
using System.Windows.Forms;
using System.Xml;
using System.Threading;
using Cognex.VisionPro.PMAlign;
using Cognex.VisionPro;
using System.Diagnostics;
using Accord.Math;
using System.IO;

namespace PacTech.uc
{
	public partial class ucCameraCheck : UserControl
	{
		public enum ParameterType
		{
			ParallaxDiscrepancyX,
			ParallaxDiscrepancyY,
			InspectionCameraToleranceX,
			InspectionCameraToleranceY,
			InspectionCameraToleranceR,
			AutoAdjustToleranceX,
			AutoAdjustToleranceY,
			AutoAdjustMaxRetries,
			BottomCameraToleranceX,
			BottomCameraToleranceY,
			BottomCameraToleranceR,
			BottomCameraOffsetHairCrossX,
			BottomCameraOffsetHairCrossY,
			InitialSetupOffsetX,
			InitialSetupOffsetY
		}
		private void UpdatePositions(Control control=null)
		{
			if(control != null)
			{
				if ((control.Name == ParameterType.BottomCameraOffsetHairCrossX.ToString()) || (control.Name == ParameterType.BottomCameraOffsetHairCrossY.ToString()))
					UpdateOffsetHairCross();
			}
			_ucXYZTInspectionCameraCheckXYZ.updateParameter();
			_ucXYZTVisionCameraCheckXYZ.updateParameter();
		}

		private void CustomInit()
		{
			try
			{
				_ucXYZTInitialInspectionCameraCheckXYZ.SetName("Initial Position");
			}
			catch (Exception ex)
			{
				Utils.Utils.LogException(ex, GetType());
			}
		}
		public void updateParameter()
		{
			try
			{
				foreach (var param in Parameters)
				{
					Control control = this.Controls.Find(param.ControlName, true).FirstOrDefault();
					if (control != null)
					{
						if (control is TextBox textBox)
						{
							bool noDecimals = false;
							if (param.ControlName == ParameterType.AutoAdjustMaxRetries.ToString())
							{
								noDecimals = true;
							}
							Utils.Utils.UpdateTextBox(textBox, param.controlValuePair.Value, noDecimal: noDecimals);
						}
						else if (control is CheckBox checkBox)
						{
							bool isChecked = Convert.ToBoolean(param.controlValuePair.Value);
							checkBox.Checked = isChecked;
						}
					}
				}
			}
			catch (Exception ex)
			{
				Utils.Utils.LogException(ex, GetType());
			}
		}

		#region ucDefaults
		public class ParameterName
		{
			public ParameterType Type { get; }
			public string Name { get; }
			public double DefaultValue { get; }

			public ParameterName(ParameterType type, double defaultValue = 0.0)
			{
				Type = type;
				Name = type.ToString();
				DefaultValue = defaultValue;
			}

			public static Dictionary<ParameterType, ParameterName> AllParameters { get; } = InitializeAllParameters();

			private static Dictionary<ParameterType, ParameterName> InitializeAllParameters()
			{
				var parameters = new Dictionary<ParameterType, ParameterName>();

				foreach (ParameterType type in Enum.GetValues(typeof(ParameterType)))
				{
					parameters.Add(type, new ParameterName(type));
				}
				return parameters;
			}
		}

		private static List<ParameterValue> GenerateParameters()
		{
			List<ParameterValue> parameters = new List<ParameterValue>();
			try
			{
				foreach (var pair in ParameterName.AllParameters)
				{
					parameters.Add(new ParameterValue(pair.Value.Name, new ControlValuePair(null, pair.Value.DefaultValue)));
				}
			}
			catch (Exception ex)
			{
				Utils.Utils.LogException(ex);
			}
			return parameters;
		}

		private List<ParameterValue> Parameters { get; set; } = GenerateParameters();

		private ucCameraCheck linkedControl;
		public void LinkControl(ucCameraCheck controlToLink)
		{
			linkedControl = controlToLink;
			controlToLink.linkedControl = this;
		}
		public ucCameraCheck()
		{
			try
			{
				InitializeComponent();
				this.DoubleBuffered = true;
				foreach (var param in Parameters)
				{
					Control control = this.Controls.Find(param.ControlName, true).FirstOrDefault();
					if (control != null)
					{
						if (control is TextBox textBox)
						{
							textBox.TextChanged += TextBox_TextChanged;
						}
						else if (control is CheckBox checkBox)
						{
							checkBox.CheckedChanged += CheckBox_CheckedChanged;
						}
					}
				}
				CustomInit();
			}
			catch (Exception ex)
			{
				Utils.Utils.LogException(ex, GetType());
			}
		}

		private void TextBox_TextChanged(object sender, EventArgs e)
		{
			try
			{
				UserControlUtils.HandleTextChanged(sender, Parameters, linkedControl);
				UpdatePositions((Control)sender);
			}
			catch (Exception ex)
			{
				Utils.Utils.LogException(ex, GetType());
			}
		}

		private void CheckBox_CheckedChanged(object sender, EventArgs e)
		{
			try
			{
				UserControlUtils.HandleCheckedChanged(sender, Parameters, linkedControl);
				UpdatePositions((Control)sender);
			}
			catch (Exception ex)
			{
				Utils.Utils.LogException(ex, GetType());
			}
		}

		public bool TryGetValidatedControlValue(string controlName, out double value, Func<double, double> transformation = null, bool checkZero = true)
		{
			value = 0.0;
			try
			{
				return UserControlUtils.TryGetValidatedControlValue(this, Parameters, controlName, out value, transformation, checkZero);
			}
			catch (Exception ex)
			{
				Utils.Utils.LogException(ex);
				return false;
			}
		}
		public bool TryGetValidatedControlValue(ParameterType type, out double value, Func<double, double> transformation = null, bool checkZero = true)
		{
			value = 0.0;
			try
			{
				string controlName = type.ToString();
				return UserControlUtils.TryGetValidatedControlValue(this, Parameters, controlName, out value, transformation, checkZero);
			}
			catch (Exception ex)
			{
				Utils.Utils.LogException(ex);
				return false;
			}
		}
		public bool TryGetValidatedControlValue(string controlName, out bool value)
		{
			value = false;
			try
			{
				return UserControlUtils.TryGetValidatedControlValue(this, Parameters, controlName, out value);
			}
			catch (Exception ex)
			{
				Utils.Utils.LogException(ex);
				return false;
			}
		}
		public bool TryGetValidatedControlValue(ParameterType type, out int value)
		{
			value = 0;
			try
			{
				return UserControlUtils.TryGetValidatedControlValue(this, Parameters, type.ToString(), out value);
			}
			catch (Exception ex)
			{
				Utils.Utils.LogException(ex);
				return false;
			}
		}

		public bool TrySetControlValue(string controlName, double newValue)
		{
			try
			{
				return UserControlUtils.TrySetControlValue(this, Parameters, controlName, newValue);
			}
			catch (Exception ex)
			{
				Utils.Utils.LogException(ex, GetType());
				return false;
			}
		}

		public bool TrySetControlValue(string controlName, bool newValue)
		{
			double convertedValue = newValue ? 1.0 : 0.0;
			return TrySetControlValue(controlName, convertedValue);
		}

		public void WriteSettings(XmlTextWriter writer)
		{
			try
			{
				// Start the parent element.
				writer.WriteStartElement(this.Name);

				// Write the parameters.
				foreach (var param in Parameters)
				{
					Control control = this.Controls.Find(param.ControlName, true).FirstOrDefault();
					if (control != null)
					{
						switch (control)
						{
							case TextBox textBox:
								writer.WriteElementString(param.ControlName, textBox.Text);
								break;
							case CheckBox checkBox:
								writer.WriteElementString(param.ControlName, checkBox.Checked.ToString());
								break;
						}
					}
				}

				// Write settings for all child UserControls.
				foreach (Control control in this.Controls)
				{
					if (control is UserControl userControl)
					{
						Utils.Utils.InvokeFunctionIfExists(userControl, "WriteSettings", writer);
					}
				}

				// End the parent element.
				writer.WriteEndElement();
			}
			catch (Exception ex)
			{
				Utils.Utils.LogException(ex);
			}
		}
		public void ReadSettings(XmlDocument xmlDocument)
		{
			try
			{
				XmlNode xmlNode = xmlDocument.SelectSingleNode($"/Recipe/{Name}");
				if (xmlNode != null)
				{
					foreach (var param in Parameters)
					{
						Control control = this.Controls.Find(param.ControlName, true).FirstOrDefault();
						if (control != null && control is TextBox)
						{
							if (xmlNode[param.ControlName] != null)
								param.controlValuePair.Value = Utils.Utils.ConvertToDouble(xmlNode[param.ControlName].InnerText.Trim());
						}
					}
				}
				updateParameter();

				// Read settings for all child UserControls.
				foreach (Control control in this.Controls)
				{
					if (control is UserControl userControl)
					{
						Utils.Utils.InvokeFunctionIfExists(userControl, "ReadSettings", xmlDocument);
					}
				}
			}
			catch (Exception ex)
			{
				Utils.Utils.LogException(ex, GetType());
			}
		}
		#endregion

		private struct CameraParameters
		{
			public userControlCameras UserControlCamera { get; set; }
			public string VisionStep { get; set; }
			public double Resolution { get; set; }
		}

		private Dictionary<CameraType, CameraParameters> cameraParametersDictionary = new Dictionary<CameraType, CameraParameters>();

		private void InitializeCameraParametersDictionary()
		{
			InspectionFrm insp = InspectionFrm.insp;
			cameraParametersDictionary[CameraType.BottomCamera] = new CameraParameters
			{
				UserControlCamera = insp.userControlCamera3,
				VisionStep = VisionStepsNames.BottomCameraCheckXYZ.ToString(),
				Resolution = Utils.Utils.ConvertToDouble(insp.userControlSetupCamera3.Resolution) / 1000.0
			};
			cameraParametersDictionary[CameraType.InspectionCamera] = new CameraParameters
			{
				UserControlCamera = insp.userControlCamera2,
				VisionStep = VisionStepsNames.InspectionCameraCheckXYZ.ToString(),
				Resolution = Utils.Utils.ConvertToDouble(insp.userControlSetupCamera2.Resolution) / 1000.0
			};
		}

		private int CameraCheckXYZ(CameraType cameraType)
		{
			if (!cameraParametersDictionary.ContainsKey(cameraType))
			{
				return -1;
			}

			var cameraParameters = cameraParametersDictionary[cameraType];

			try
			{
				if (PacTech.Utils.Utils.Stop) { Utils.Utils.LogException(new Exception("Stop activated"), GetType()); return -1; }

				userControlCameras userControlCamera = cameraParameters.UserControlCamera;
				string visionStep = cameraParameters.VisionStep;
				double Resolution = cameraParameters.Resolution;

				InspectionFrm insp = InspectionFrm.insp;
				userControlVision userControlVision1 = insp.userControlVision1;

				if (!TryGetValidatedControlValue($"{cameraType}ToleranceX", out double ToleranceX, checkZero: false))
					return -1;
				if (!TryGetValidatedControlValue($"{cameraType}ToleranceY", out double ToleranceY, checkZero: false))
					return -1;
				if (!TryGetValidatedControlValue($"{cameraType}ToleranceR", out double ToleranceR, checkZero: false))
					return -1;

				if (!userControlVision1.RunPMAlignTool(visionStep, out CogPMAlignTool cogPMAlignTool, userControlCamera))
					return -1;

				CogTransform2DLinear cogTransform2DLinearResult = cogPMAlignTool.Results[0].GetPose();

				double imageWidth = userControlCamera.CurrentImage.Width;
				double imageHeight = userControlCamera.CurrentImage.Height;

				double pixShiftX = imageWidth / 2 - cogTransform2DLinearResult.TranslationX;
				double pixShiftY = imageHeight / 2 - cogTransform2DLinearResult.TranslationY;
				userControlCamera.xShift = pixShiftX;
				userControlCamera.yShift = pixShiftY;
				double shiftX = pixShiftX * Resolution;
				double shiftY = pixShiftY * Resolution;

				double absoluteShiftX = Math.Abs(shiftX);
				double absoluteShiftY = Math.Abs(shiftY);
				double Rotation = Math.Abs(cogTransform2DLinearResult.Rotation);
				if (absoluteShiftX > ToleranceX || absoluteShiftY > ToleranceY || Rotation > ToleranceR)
				{
					userControlCamera.DrawHairCross(CogColorConstants.Red);
					LogVisionStepOutOfRange(visionStep, "OffsetX", shiftX, ToleranceX, absoluteShiftX);
					LogVisionStepOutOfRange(visionStep, "OffsetY", shiftY, ToleranceY, absoluteShiftY);
					LogVisionStepOutOfRange(visionStep, "Rotation", Rotation, ToleranceR, Rotation);
					return -1;
				}

				if (cameraType == CameraType.BottomCamera)
				{
					if (!TrySetControlValue(ParameterType.BottomCameraOffsetHairCrossX.ToString(), shiftX))
						return -1;
					if (!TrySetControlValue(ParameterType.BottomCameraOffsetHairCrossY.ToString(), shiftY))
						return -1;
					userControlCamera.DrawHairCross(CogColorConstants.Green);
					Trace.WriteLine($"{visionStep} OffsetX:{shiftX:0.000} OffsetY:{shiftY:0.000} within Tolerance X:{ToleranceX:0.000} Y:{ToleranceY:0.000}");
				}
				else if (cameraType == CameraType.InspectionCamera)
				{
					return ProcessInspectionCameraCheck(visionStep, userControlCamera, shiftX, shiftY, ToleranceX, ToleranceY);
				}

				return 1;
			}
			catch (Exception ex)
			{
				Utils.Utils.LogException(ex, GetType(), preMessage: $"{cameraType}");
				return -1;
			}
		}

		private int ProcessInspectionCameraCheck(string visionStep, userControlCameras userControlCamera, double shiftX, double shiftY, double ToleranceX, double ToleranceY)
		{
			if (!TryGetValidatedControlValue(ParameterType.AutoAdjustToleranceX, out double AutoAdjustToleranceX, checkZero: false))
				return -1;
			if (!TryGetValidatedControlValue(ParameterType.AutoAdjustToleranceY, out double AutoAdjustToleranceY, checkZero: false))
				return -1;
			double absoluteShiftX = Math.Abs(shiftX);
			double absoluteShiftY = Math.Abs(shiftY);
			if (absoluteShiftX > AutoAdjustToleranceX || absoluteShiftY > AutoAdjustToleranceY)
			{
				_ucXYZTInspectionCameraCheckXYZ.IncrementValues(X: shiftX, Y: shiftY);
				userControlOffsets userControlOffsets1 = InspectionFrm.insp.userControlOffsets1;
				if (!userControlOffsets1.GetProcessAxisPosition(WiresPositions.InspectionCameraCheckXYZ, out double paX, out double paY, out double paZ))
					return -1;
				if (!userControlOffsets1.GetProcessAxisPosition(WiresPositions.InitialInspectionCameraCheckXYZ, out double paInitX, out double paInitY, out double paInitZ))
					return -1;
				double absoluteDistX = Math.Abs(paInitX - paX);
				double absoluteDistY = Math.Abs(paInitY - paY);
				if (absoluteDistX > ToleranceX || absoluteDistY > ToleranceY)
				{
					userControlCamera.DrawHairCross(CogColorConstants.Red);
					if (absoluteShiftX > ToleranceX)
					{
						Trace.WriteLine($"{visionStep} OffsetX:{absoluteDistX:0.000} out of ToleranceX:{ToleranceX:0.000}");
					}
					if (absoluteShiftY > ToleranceY)
					{
						Trace.WriteLine($"{visionStep} OffsetY:{absoluteDistY:0.000} out of ToleranceY:{ToleranceY:0.000}");
					}
					return -1;
				}
				return 1;
			}
			else
				return 0;
		}

		private void LogVisionStepOutOfRange(string visionStep, string direction, double observedValue, double tolerance, double absoluteValue)
		{
			string message = $"{visionStep} {direction}:{observedValue:0.000} out of {direction}:{tolerance:0.000} Actual:{absoluteValue:0.000}";
			Trace.WriteLine(message);
		}


		public bool InspectionCameraCheckXYZ()
		{
			try
			{
				string ProcessStep = "InspectionCameraCheckXYZ";
				if (PacTech.Utils.Utils.Stop) { Utils.Utils.LogException(new Exception("Stop activated"), GetType()); return false; }

				InspectionFrm insp = InspectionFrm.insp;
				AerotechControl.AerotechControl aerotechControl = insp.aerotechControl;
				userControlOffsets userControlOffsets1 = insp.userControlOffsets1;
				ucRun _ucRun = insp._ucRun;

				insp.InvokeSwitchCameraTab(CameraType.BottomCamera);
				if (!userControlOffsets1.GetProcessAxisPosition("ParkPosition", out double paX, out double paY, out double paParkZ))
					return false;
				if (!aerotechControl.MoveToZTask(paParkZ))
					return false;
				_ucRun.Update(ProcessStep, 20);
				if (!aerotechControl.SetBO("table crosshair LEFT/RIGHT", true, "table crosshair RIGHT", 1000, true))
					return false;
				Thread.Sleep(1500);
				int response = CameraCheckXYZ(CameraType.BottomCamera);
				insp.userControlCamera3.SaveImageInSubdirectory(ProcessStep);

				_ucRun.Update(ProcessStep, 30);

				insp.InvokeSwitchCameraTab(CameraType.MachineCamera);
				if (!userControlOffsets1.GetProcessAxisPosition(WiresPositions.InspectionCameraCheckXYZ, out paX, out paY, out double paZ))
					return false;
				_ucRun.Update(ProcessStep, 40);
				if (!aerotechControl.MoveToPos(paX, paY, paParkZ))
					return false;
				if (!aerotechControl.SetBO("table crosshair LEFT/RIGHT", true, "table crosshair RIGHT", 1000, true))
					return false;
				_ucRun.Update(ProcessStep, 50);
				if (!aerotechControl.WaitForInPosition(paX, paY, paParkZ, timeout: 10000))
					return false;
				_ucRun.Update(ProcessStep, 60);
				if (!aerotechControl.MoveToPos(paX, paY, paZ, _async: false))
					return false;
				_ucRun.Update(ProcessStep, 70);

				insp.InvokeSwitchCameraTab(CameraType.InspectionCamera);
				_ucRun.Update(ProcessStep, 80);
				int response2 = CameraCheckXYZ(CameraType.InspectionCamera);
				if (!TryGetValidatedControlValue(ParameterType.AutoAdjustMaxRetries, out int AutoAdjustMaxRetries))
					return false;
				while (--AutoAdjustMaxRetries > 0 && response2 != 0)
				{
					insp.userControlCamera2.SaveImageInSubdirectory(ProcessStep);
					aerotechControl.MoveToPosXY(paX, paY, Prepare: false);
					response2 = CameraCheckXYZ(CameraType.InspectionCamera);
				}
				if (response != 0 || response2 != 0)
					return false;
				_ucRun.Update(ProcessStep, 90);
				return true;
			}
			catch (Exception ex)
			{
				Utils.Utils.LogException(ex, GetType());
				return false;
			}
		}

		private bool UpdateOffsetHairCross()
		{
			try
			{
				InspectionFrm insp = InspectionFrm.insp;
				double ResolutionInspectionCamera = Utils.Utils.ConvertToDouble(insp.userControlSetupCamera2.Resolution) /1000.0;
				double ResolutionBottomCamera = Utils.Utils.ConvertToDouble(insp.userControlSetupCamera3.Resolution) / 1000.0;

				if (!TryGetValidatedControlValue(ParameterType.BottomCameraOffsetHairCrossX.ToString(), out double pixBottomCameraOffsetHairCrossX, value => value / ResolutionBottomCamera, checkZero: false))
					return false;
				if (!TryGetValidatedControlValue(ParameterType.BottomCameraOffsetHairCrossY.ToString(), out double pixBottomCameraOffsetHairCrossY, value => value / ResolutionBottomCamera, checkZero: false))
					return false;
				InspectionFrm.insp.userControlCamera3.xShift = pixBottomCameraOffsetHairCrossX;
				InspectionFrm.insp.userControlCamera3.yShift = pixBottomCameraOffsetHairCrossY;
				InspectionFrm.insp.userControlCamera2.xShift = -pixBottomCameraOffsetHairCrossX * ResolutionBottomCamera / ResolutionInspectionCamera;
				InspectionFrm.insp.userControlCamera2.yShift = -pixBottomCameraOffsetHairCrossY * ResolutionBottomCamera / ResolutionInspectionCamera;
				return true;
			}
			catch (Exception ex)
			{
				Utils.Utils.LogException(ex, GetType());
				return false;
			}
		}

	}
}
