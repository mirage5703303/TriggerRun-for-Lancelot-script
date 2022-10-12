using System;

namespace IEdgeGames {

    public class CellContext {
        public int SelectedIndex = -1;
        public Action<int> OnCellClicked;
    }
}
