namespace Cmp.Viewer.Win
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend2 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea3 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend3 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series3 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea4 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend4 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series4 = new System.Windows.Forms.DataVisualization.Charting.Series();
            splitRoot = new SplitContainer();
            tableLayoutchartGridPanel1 = new TableLayoutPanel();
            chartPress = new System.Windows.Forms.DataVisualization.Charting.Chart();
            chartMean = new System.Windows.Forms.DataVisualization.Charting.Chart();
            chartWiw = new System.Windows.Forms.DataVisualization.Charting.Chart();
            chartRange = new System.Windows.Forms.DataVisualization.Charting.Chart();
            bottomGrid = new TableLayoutPanel();
            grpStatus = new GroupBox();
            lblInitWiw = new Label();
            label9 = new Label();
            lblInitRange = new Label();
            lblStartTime = new Label();
            lblWaferId = new Label();
            lblInitMean = new Label();
            label4 = new Label();
            label3 = new Label();
            label2 = new Label();
            label1 = new Label();
            chartGrid = new TableLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)splitRoot).BeginInit();
            splitRoot.Panel1.SuspendLayout();
            splitRoot.Panel2.SuspendLayout();
            splitRoot.SuspendLayout();
            tableLayoutchartGridPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)chartPress).BeginInit();
            ((System.ComponentModel.ISupportInitialize)chartMean).BeginInit();
            ((System.ComponentModel.ISupportInitialize)chartWiw).BeginInit();
            ((System.ComponentModel.ISupportInitialize)chartRange).BeginInit();
            bottomGrid.SuspendLayout();
            grpStatus.SuspendLayout();
            SuspendLayout();
            // 
            // splitRoot
            // 
            splitRoot.Dock = DockStyle.Fill;
            splitRoot.FixedPanel = FixedPanel.Panel2;
            splitRoot.Location = new Point(0, 0);
            splitRoot.Name = "splitRoot";
            splitRoot.Orientation = Orientation.Horizontal;
            // 
            // splitRoot.Panel1
            // 
            splitRoot.Panel1.Controls.Add(tableLayoutchartGridPanel1);
            // 
            // splitRoot.Panel2
            // 
            splitRoot.Panel2.Controls.Add(bottomGrid);
            splitRoot.Panel2MinSize = 80;
            splitRoot.Size = new Size(591, 583);
            splitRoot.SplitterDistance = 498;
            splitRoot.TabIndex = 0;
            // 
            // tableLayoutchartGridPanel1
            // 
            tableLayoutchartGridPanel1.ColumnCount = 2;
            tableLayoutchartGridPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutchartGridPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutchartGridPanel1.Controls.Add(chartPress, 0, 0);
            tableLayoutchartGridPanel1.Controls.Add(chartMean, 1, 0);
            tableLayoutchartGridPanel1.Controls.Add(chartWiw, 0, 1);
            tableLayoutchartGridPanel1.Controls.Add(chartRange, 1, 1);
            tableLayoutchartGridPanel1.Dock = DockStyle.Fill;
            tableLayoutchartGridPanel1.Location = new Point(0, 0);
            tableLayoutchartGridPanel1.Name = "tableLayoutchartGridPanel1";
            tableLayoutchartGridPanel1.RowCount = 2;
            tableLayoutchartGridPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutchartGridPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutchartGridPanel1.Size = new Size(591, 498);
            tableLayoutchartGridPanel1.TabIndex = 0;
            // 
            // chartPress
            // 
            chartArea1.Name = "ChartArea1";
            chartPress.ChartAreas.Add(chartArea1);
            chartPress.Dock = DockStyle.Fill;
            legend1.Name = "Legend1";
            chartPress.Legends.Add(legend1);
            chartPress.Location = new Point(3, 3);
            chartPress.Name = "chartPress";
            series1.ChartArea = "ChartArea1";
            series1.Legend = "Legend1";
            series1.Name = "Series1";
            chartPress.Series.Add(series1);
            chartPress.Size = new Size(289, 243);
            chartPress.TabIndex = 0;
            chartPress.Text = "chart1";
            // 
            // chartMean
            // 
            chartArea2.Name = "ChartArea1";
            chartMean.ChartAreas.Add(chartArea2);
            chartMean.Dock = DockStyle.Fill;
            legend2.Name = "Legend1";
            chartMean.Legends.Add(legend2);
            chartMean.Location = new Point(298, 3);
            chartMean.Name = "chartMean";
            series2.ChartArea = "ChartArea1";
            series2.Legend = "Legend1";
            series2.Name = "Series1";
            chartMean.Series.Add(series2);
            chartMean.Size = new Size(290, 243);
            chartMean.TabIndex = 1;
            chartMean.Text = "chart2";
            // 
            // chartWiw
            // 
            chartArea3.Name = "ChartArea1";
            chartWiw.ChartAreas.Add(chartArea3);
            chartWiw.Dock = DockStyle.Fill;
            legend3.Name = "Legend1";
            chartWiw.Legends.Add(legend3);
            chartWiw.Location = new Point(3, 252);
            chartWiw.Name = "chartWiw";
            series3.ChartArea = "ChartArea1";
            series3.Legend = "Legend1";
            series3.Name = "Series1";
            chartWiw.Series.Add(series3);
            chartWiw.Size = new Size(289, 243);
            chartWiw.TabIndex = 2;
            chartWiw.Text = "chart3";
            // 
            // chartRange
            // 
            chartArea4.Name = "ChartArea1";
            chartRange.ChartAreas.Add(chartArea4);
            chartRange.Dock = DockStyle.Fill;
            legend4.Name = "Legend1";
            chartRange.Legends.Add(legend4);
            chartRange.Location = new Point(298, 252);
            chartRange.Name = "chartRange";
            series4.ChartArea = "ChartArea1";
            series4.Legend = "Legend1";
            series4.Name = "Series1";
            chartRange.Series.Add(series4);
            chartRange.Size = new Size(290, 243);
            chartRange.TabIndex = 3;
            chartRange.Text = "chart4";
            // 
            // bottomGrid
            // 
            bottomGrid.ColumnCount = 1;
            bottomGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 76.31134F));
            bottomGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23.6886635F));
            bottomGrid.Controls.Add(grpStatus, 0, 0);
            bottomGrid.Dock = DockStyle.Fill;
            bottomGrid.Location = new Point(0, 0);
            bottomGrid.Name = "bottomGrid";
            bottomGrid.RowCount = 1;
            bottomGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 62F));
            bottomGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));
            bottomGrid.Size = new Size(591, 81);
            bottomGrid.TabIndex = 0;
            bottomGrid.Paint += bottomGrid_Paint;
            // 
            // grpStatus
            // 
            grpStatus.Controls.Add(lblInitWiw);
            grpStatus.Controls.Add(label9);
            grpStatus.Controls.Add(lblInitRange);
            grpStatus.Controls.Add(lblStartTime);
            grpStatus.Controls.Add(lblWaferId);
            grpStatus.Controls.Add(lblInitMean);
            grpStatus.Controls.Add(label4);
            grpStatus.Controls.Add(label3);
            grpStatus.Controls.Add(label2);
            grpStatus.Controls.Add(label1);
            grpStatus.Dock = DockStyle.Fill;
            grpStatus.Location = new Point(3, 3);
            grpStatus.Name = "grpStatus";
            grpStatus.Size = new Size(585, 75);
            grpStatus.TabIndex = 0;
            grpStatus.TabStop = false;
            grpStatus.Text = "Current Wafer Status";
            // 
            // lblInitWiw
            // 
            lblInitWiw.AutoSize = true;
            lblInitWiw.Location = new Point(392, 48);
            lblInitWiw.Name = "lblInitWiw";
            lblInitWiw.Size = new Size(12, 15);
            lblInitWiw.TabIndex = 9;
            lblInitWiw.Text = "-";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(309, 48);
            label9.Name = "label9";
            label9.Size = new Size(77, 15);
            label9.TabIndex = 8;
            label9.Text = "Init WIWNU :";
            // 
            // lblInitRange
            // 
            lblInitRange.AutoSize = true;
            lblInitRange.Location = new Point(232, 48);
            lblInitRange.Name = "lblInitRange";
            lblInitRange.Size = new Size(12, 15);
            lblInitRange.TabIndex = 7;
            lblInitRange.Text = "-";
            // 
            // lblStartTime
            // 
            lblStartTime.AutoSize = true;
            lblStartTime.Location = new Point(232, 19);
            lblStartTime.Name = "lblStartTime";
            lblStartTime.Size = new Size(12, 15);
            lblStartTime.TabIndex = 6;
            lblStartTime.Text = "-";
            // 
            // lblWaferId
            // 
            lblWaferId.AutoSize = true;
            lblWaferId.Location = new Point(76, 19);
            lblWaferId.Name = "lblWaferId";
            lblWaferId.Size = new Size(12, 15);
            lblWaferId.TabIndex = 5;
            lblWaferId.Text = "-";
            // 
            // lblInitMean
            // 
            lblInitMean.AutoSize = true;
            lblInitMean.Location = new Point(80, 48);
            lblInitMean.Name = "lblInitMean";
            lblInitMean.Size = new Size(12, 15);
            lblInitMean.TabIndex = 4;
            lblInitMean.Text = "-";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(158, 48);
            label4.Name = "label4";
            label4.Size = new Size(68, 15);
            label4.TabIndex = 3;
            label4.Text = "Init Range :";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(9, 48);
            label3.Name = "label3";
            label3.Size = new Size(65, 15);
            label3.TabIndex = 2;
            label3.Text = "Init Mean :";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(157, 19);
            label2.Name = "label2";
            label2.Size = new Size(69, 15);
            label2.TabIndex = 1;
            label2.Text = "Start Time :";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(9, 19);
            label1.Name = "label1";
            label1.Size = new Size(61, 15);
            label1.TabIndex = 0;
            label1.Text = "Wafer ID :";
            // 
            // chartGrid
            // 
            chartGrid.ColumnCount = 2;
            chartGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            chartGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            chartGrid.Dock = DockStyle.Fill;
            chartGrid.Location = new Point(0, 0);
            chartGrid.Name = "chartGrid";
            chartGrid.RowCount = 2;
            chartGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            chartGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            chartGrid.Size = new Size(591, 498);
            chartGrid.TabIndex = 0;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(591, 583);
            Controls.Add(splitRoot);
            Name = "MainForm";
            Text = "CMP Dashboard";
            Load += MainForm_Load;
            splitRoot.Panel1.ResumeLayout(false);
            splitRoot.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitRoot).EndInit();
            splitRoot.ResumeLayout(false);
            tableLayoutchartGridPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)chartPress).EndInit();
            ((System.ComponentModel.ISupportInitialize)chartMean).EndInit();
            ((System.ComponentModel.ISupportInitialize)chartWiw).EndInit();
            ((System.ComponentModel.ISupportInitialize)chartRange).EndInit();
            bottomGrid.ResumeLayout(false);
            grpStatus.ResumeLayout(false);
            grpStatus.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitRoot;
        private TableLayoutPanel chartGrid;
        private TableLayoutPanel bottomGrid;
        private GroupBox grpStatus;
        private Label label3;
        private Label label2;
        private Label label1;
        private Label lblWaferId;
        private Label lblInitMean;
        private Label label4;
        private Label lblInitWiw;
        private Label label9;
        private Label lblInitRange;
        private Label lblStartTime;
        private TableLayoutPanel tableLayoutchartGridPanel1;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartPress;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartMean;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartWiw;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartRange;
    }
}