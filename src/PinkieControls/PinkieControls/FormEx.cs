using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace PinkieControls
{
	public class FormEx : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Timer timer;
		private float fadeTime = 10.0f;
		private System.ComponentModel.Container _components = null;

		[DefaultValue(1.0), Browsable(true)]
		public float FadeTime
		{
			get { return fadeTime; }
			set { fadeTime = value; }
		}

		public FormEx()
		{
			InitializeComponent();
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				if(_components != null)
				{
					_components.Dispose();
				}
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		private void InitializeComponent()
		{
			this._components = new System.ComponentModel.Container();
			this.timer = new System.Windows.Forms.Timer();
			// 
			// timer
			// 
			this.timer.Tick += new System.EventHandler(this.timer_Tick);
			// 
			// FormEx
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(292, 266);
			this.Name = "FormEx";
			this.Text = "Form1";
		}

		#endregion

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			if(!DesignMode)
			{
				Opacity = 0;
				timer.Enabled = true;
			}
		}

		private void timer_Tick(object sender, System.EventArgs e)
		{
			Opacity += (timer.Interval / fadeTime);
			timer.Enabled = (Opacity < 1.0);
		}
	}
}