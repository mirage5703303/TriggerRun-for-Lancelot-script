using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace IEdgeGames {

	internal static class UIModuleControl {
		public static readonly List<MonoBehaviour> Modules = new List<MonoBehaviour>();
	}

	[RequireComponent(typeof(Canvas))]
	[RequireComponent(typeof(GraphicRaycaster))]
	public abstract class UIModule<T> : MonoBehaviour where T : MonoBehaviour {

		[SerializeField, BoxGroup("UI Module")] 
		protected bool m_ActiveOnAwake;

		[SerializeField, BoxGroup("UI Module")]
		protected bool m_IsPersistent;

		protected Canvas m_Canvas;
		protected static T m_Instance;

		/// <summary>
		/// Singleton instance.
		/// </summary>
		public static T Instance => m_Instance ? m_Instance : (m_Instance = FindObjectOfType<T>());

		/// <summary>
		/// 
		/// </summary>
		public static bool IsPersistent => Instance ? (Instance as UIModule<T>).m_IsPersistent : false;

		/// <summary>
		/// Enable/disable this module.
		/// </summary>
		public static bool Active {
			get => Instance ? (Instance as UIModule<T>).m_Canvas.enabled : false;
			set {
				if (!Instance)
					return;

				var instanceType = Instance.GetType();

				UIModuleControl.Modules.ForEach(m => {
					var moduleType = m.GetType();
					var module = moduleType.BaseType;
					var isPersistent = (bool)module.GetField("m_IsPersistent", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(m);

					if (instanceType == moduleType || isPersistent)
						return;

					var setActive = module.GetMethod("SetActive", BindingFlags.Instance | BindingFlags.NonPublic);
					setActive.Invoke(m, new object[] { false });
				});

				(Instance as UIModule<T>).SetActive(value);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="active"></param>
		/// <param name="callEvent"></param>
		private void SetActive(bool active) {
			if (!m_Canvas)
				return;

			m_Canvas.enabled = active;

			if (active)
				OnModuleEnable();
			else
				OnModuleDisable();
		}

		/// <summary>
		/// 
		/// </summary>
		private void OnEnable() {
			if (!m_Canvas) {
				m_Canvas = GetComponent<Canvas>();
				OnModuleStart();
				SetActive(m_ActiveOnAwake);
			}

			if (!UIModuleControl.Modules.Exists(m => m == this))
				UIModuleControl.Modules.Add(this);
		}

		/// <summary>
		/// 
		/// </summary>
		private void OnDisable() {
			if (UIModuleControl.Modules.Exists(m => m == this))
				UIModuleControl.Modules.Remove(this);
		}

		/// <summary>
		/// 
		/// </summary>
		private void OnDestroy() => OnModuleDestroy();

		/// <summary>
		/// 
		/// </summary>
		protected virtual void OnModuleStart() { }

		/// <summary>
		/// 
		/// </summary>
		protected virtual void OnModuleEnable() { }

		/// <summary>
		/// 
		/// </summary>
		protected virtual void OnModuleDisable() { }

		/// <summary>
		/// 
		/// </summary>
		protected virtual void OnModuleDestroy() { }
	}
}
