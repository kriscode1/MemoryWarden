using System.Windows.Controls;
using System.Windows.Media;

namespace MemoryWarden
{
    class TableFormatter
    {
        //Helps keep the two tables have consistent styles.

        private DataGrid table;
        private static Brush LIGHT_GRAY, BLACK, TRANSPARENT, LIGHT_BLUE, STEEL_BLUE, LIGHT_CYAN, VIOLET, WHITE, HOT_PINK, GREEN;
        private static Brush SELECTED_ROW_BG, SELECTED_ROW_BORDER, HOVER_ROW_BG, HOVER_ROW_BORDER;

        public TableFormatter(DataGrid table)
        {
            this.table = table;

            //Initialize brushes if not already
            if (LIGHT_GRAY == null)
            {
                //Prebuilts, mostly for testing
                LIGHT_GRAY = new SolidColorBrush(Colors.LightGray);
                BLACK = new SolidColorBrush(Colors.Black);
                TRANSPARENT = new SolidColorBrush(Colors.Transparent);
                LIGHT_BLUE = new SolidColorBrush(Colors.LightBlue);
                STEEL_BLUE = new SolidColorBrush(Colors.SteelBlue);
                LIGHT_CYAN = new SolidColorBrush(Colors.LightCyan);
                VIOLET = new SolidColorBrush(Colors.Violet);
                WHITE = new SolidColorBrush(Colors.White);
                HOT_PINK = new SolidColorBrush(Colors.HotPink);
                GREEN = new SolidColorBrush(Colors.Green);

                //Customs
                SELECTED_ROW_BG = new SolidColorBrush(Color.FromRgb(209, 232, 255));
                SELECTED_ROW_BORDER = new SolidColorBrush(Color.FromRgb(38, 160, 218));
                HOVER_ROW_BG = new SolidColorBrush(Color.FromRgb(229, 243, 251));
                HOVER_ROW_BORDER = new SolidColorBrush(Color.FromRgb(112, 192, 231));
            }
        }

        //Prebuilt settings to call in an event handler
        public void CellSelected(DataGridCell cell)
        {
            //Selected cells have style priority over selected rows.
            cell.Background = SELECTED_ROW_BG;
            cell.Foreground = BLACK;//this controls the selected foreground
        }
        public void CellUnselected(DataGridCell cell)
        {
            //Cell border size is 0, only clear background.
            cell.ClearValue(DataGridCell.BackgroundProperty);
        }
        public void RowSelected(DataGridRow row)
        {
            //Cells have priority over background color, so only set the border.
            row.BorderBrush = SELECTED_ROW_BORDER;
        }
        public void RowUnselected(DataGridRow row)
        {
            //Clear border, and cell if the user clicked away while mouse wasn't over.
            row.ClearValue(DataGridRow.BackgroundProperty);
            row.ClearValue(DataGridRow.BorderBrushProperty);
        }
        public void MouseEnterRow(DataGridRow row)
        {
            //Only set the row background and border if not selected.
            //Don't need to worry about cells having priority because I
            //do not use a MouseEnterCell event.
            if (!row.IsSelected)
            {
                row.Background = HOVER_ROW_BG;
                row.BorderBrush = HOVER_ROW_BORDER;
            }
        }
        public void MouseLeaveRow(DataGridRow row)
        {
            //Only reset the row bg and border if not selected.
            //Don't need to worry about cell priority again.
            if (!row.IsSelected)
            {
                row.ClearValue(DataGridRow.BackgroundProperty);
                row.ClearValue(DataGridRow.BorderBrushProperty);
            }
        }
    }
}
