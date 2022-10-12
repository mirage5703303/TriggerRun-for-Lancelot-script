using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using TMPro;

namespace IEdgeGames {

    public class UISceneSelector : MonoBehaviour {

        [SerializeField] SceneScrollView scrollView = default;
        [SerializeField] Button prevCellButton = default;
        [SerializeField] Button nextCellButton = default;
        //[SerializeField] Text selectedItemInfo = default;
        [SerializeField] TextMeshProUGUI m_CurrentMap;

        /// <summary>
        /// 
        /// </summary>
        //public static int SelectedMapIndex { get; private set; }

        void Start() {
            prevCellButton.onClick.AddListener(scrollView.SelectPrevCell);
            nextCellButton.onClick.AddListener(scrollView.SelectNextCell);
            scrollView.OnSelectionChanged(OnSelectionChanged);

            /*var items = Enumerable.Range(0, 20)
                .Select(i => new ItemData($"Cell {i}"))
                .ToArray();*/

            scrollView.UpdateData(ProjectParameters.Maps);
            scrollView.SelectCell(0);

            Matchmaking.SelectedMap = ProjectParameters.Maps[0].map;
        }

        void OnSelectionChanged(int index) {
            //SelectedMapIndex = index;
            m_CurrentMap.text = ProjectParameters.Maps[index].map.Name;
            Matchmaking.SelectedMap = ProjectParameters.Maps[index].map;
        }
    }
}
