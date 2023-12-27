using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

// ReSharper disable All

namespace Necrofancy.PrepareProcedurally.Interface;

/// <summary>
/// PawnTable implementation without any Look Targets. Those depend on the map, which will cause the
/// game to try to look at a map that doesn't exist.
/// </summary>
/// <remarks>
/// This is a heavy modification of the PawnTable class in RimWorld - any large updates may necessitate a redo
/// </remarks>
public class MaplessPawnTable : PawnTable
{
  private static readonly Color BorderColor = new Color(1f, 1f, 1f, 0.2f);
  private PawnTableDef def;
  private Func<IEnumerable<Pawn>> pawnsGetter;
  private int minTableWidth;
  private int maxTableWidth;
  private int minTableHeight;
  private int maxTableHeight;
  private Vector2 fixedSize;
  private bool hasFixedSize;
  private bool dirty;
  private List<bool> columnAtMaxWidth = new List<bool>();
  private List<bool> columnAtOptimalWidth = new List<bool>();
  private Vector2 scrollPosition;
  private PawnColumnDef sortByColumn;
  private bool sortDescending;
  private Vector2 cachedSize;
  private List<Pawn> cachedPawns = new List<Pawn>();
  private List<float> cachedColumnWidths = new List<float>();
  private List<float> cachedRowHeights = new List<float>();
  private List<PawnColumnDef> columns = new List<PawnColumnDef>();
  private float cachedHeaderHeight;
  private float cachedHeightNoScrollbar;

  public new List<PawnColumnDef> Columns
  {
    get
    {
      this.columns.Clear();
      foreach (PawnColumnDef column in this.def.columns)
      {
        if (column.Worker.VisibleCurrently)
          this.columns.Add(column);
      }
      return this.columns;
    }
  }

  public new PawnColumnDef SortingBy => this.sortByColumn;

  public new bool SortingDescending => this.SortingBy != null && this.sortDescending;

  public new Vector2 Size
  {
    get
    {
      this.RecacheIfDirty();
      return this.cachedSize;
    }
  }

  public new float HeightNoScrollbar
  {
    get
    {
      this.RecacheIfDirty();
      return this.cachedHeightNoScrollbar;
    }
  }

  public new float HeaderHeight
  {
    get
    {
      this.RecacheIfDirty();
      return this.cachedHeaderHeight;
    }
  }

  public new List<Pawn> PawnsListForReading
  {
    get
    {
      this.RecacheIfDirty();
      return this.cachedPawns;
    }
  }

  public MaplessPawnTable(
    PawnTableDef def,
    Func<IEnumerable<Pawn>> pawnsGetter,
    int uiWidth,
    int uiHeight) : base(def, pawnsGetter, uiWidth, uiHeight)
  {
    this.def = def;
    this.pawnsGetter = pawnsGetter;
    this.SetMinMaxSize(def.minWidth, uiWidth, 0, uiHeight);
    this.SortBy(this.Columns.First(), descending: true);
    this.SetDirty();
  }

  public new void PawnTableOnGUI(Vector2 position)
  {
    if (Event.current.type == EventType.Layout)
      return;
    this.RecacheIfDirty();
    float num1 = this.cachedSize.x - 16f;
    List<PawnColumnDef> columns = this.Columns;
    int num2 = 0;
    for (int index = 0; index < columns.Count; ++index)
    {
      int width = index != columns.Count - 1 ? (int) this.cachedColumnWidths[index] : (int) ((double) num1 - (double) num2);
      Rect rect = new Rect((float) ((int) position.x + num2), (float) (int) position.y, (float) width, (float) (int) this.cachedHeaderHeight);
      columns[index].Worker.DoHeader(rect, this);
      num2 += width;
    }
    Rect outRect = new Rect((float) (int) position.x, (float) ((int) position.y + (int) this.cachedHeaderHeight), (float) (int) this.cachedSize.x, (float) ((int) this.cachedSize.y - (int) this.cachedHeaderHeight));
    Rect viewRect = new Rect(0.0f, 0.0f, outRect.width - 16f, (float) ((int) this.cachedHeightNoScrollbar - (int) this.cachedHeaderHeight));
    Widgets.BeginScrollView(outRect, ref this.scrollPosition, viewRect);
    int x = 0;
    for (int index1 = 0; index1 < columns.Count; ++index1)
    {
      int y = 0;
      PawnColumnDef pawnColumnDef = columns[index1];
      int num3 = index1 != columns.Count - 1 ? (int) this.cachedColumnWidths[index1] : (int) ((double) num1 - (double) x);
      for (int index2 = 0; index2 < this.cachedPawns.Count; ++index2)
      {
        GUI.color = BorderColor;
        Widgets.DrawLineHorizontal((float) x, (float) y, (float) num3);
        GUI.color = Color.white;
        Rect rect = new Rect((float) x, (float) y, (float) num3, (float) (int) this.cachedRowHeights[index2]);
        Pawn cachedPawn = this.cachedPawns[index2];
        bool flag = false;
        if (pawnColumnDef.groupable)
        {
          int num4 = index2;
          for (int index3 = index2 + 1; index3 < this.cachedPawns.Count && columns[index1].Worker.CanGroupWith(this.cachedPawns[index2], this.cachedPawns[index3]); ++index3)
          {
            rect.yMax += (float) (int) this.cachedRowHeights[index3];
            num4 = index3;
            flag = true;
          }
          index2 = num4;
        }
        if (((double) y - (double) this.scrollPosition.y + (double) (int) this.cachedRowHeights[index2] < 0.0 ? 1 : ((double) y - (double) this.scrollPosition.y > (double) outRect.height ? 1 : 0)) == 0)
        {
          columns[index1].Worker.DoCell(rect, cachedPawn, this);
          if (pawnColumnDef.groupable & flag)
          {
            GUI.color = BorderColor;
            Widgets.DrawLineVertical(rect.xMin, rect.yMin, rect.height);
            Widgets.DrawLineVertical(rect.xMax, rect.yMin, rect.height);
            GUI.color = Color.white;
          }
        }
        GUI.color = Color.white;
        y += (int) rect.height;
      }
      x += num3;
    }
    int y1 = 0;
    for (int index = 0; index < this.cachedPawns.Count; ++index)
    {
      Rect rect = new Rect(0.0f, (float) y1, viewRect.width, (float) (int) this.cachedRowHeights[index]);
      if (Mouse.IsOver(rect))
      {
        GUI.DrawTexture(rect, (Texture) TexUI.HighlightTex);
      }
      if (this.cachedPawns[index].Downed)
      {
        GUI.color = new Color(1f, 0.0f, 0.0f, 0.5f);
        Widgets.DrawLineHorizontal(0.0f, rect.center.y, viewRect.width);
        GUI.color = Color.white;
      }
      y1 += (int) this.cachedRowHeights[index];
    }
    Widgets.EndScrollView();
  }

  public new void SetDirty() => this.dirty = true;

  public new void SetMinMaxSize(
    int minTableWidth,
    int maxTableWidth,
    int minTableHeight,
    int maxTableHeight)
  {
    this.minTableWidth = minTableWidth;
    this.maxTableWidth = maxTableWidth;
    this.minTableHeight = minTableHeight;
    this.maxTableHeight = maxTableHeight;
    this.hasFixedSize = false;
    this.SetDirty();
  }

  public new void SetFixedSize(Vector2 size)
  {
    this.fixedSize = size;
    this.hasFixedSize = true;
    this.SetDirty();
  }

  public new void SortBy(PawnColumnDef column, bool descending)
  {
    this.sortByColumn = column;
    this.sortDescending = descending;
    this.SetDirty();
  }

  private void RecacheIfDirty()
  {
    if (!this.dirty)
      return;
    this.dirty = false;
    this.RecachePawns();
    this.RecacheRowHeights();
    this.cachedHeaderHeight = this.CalculateHeaderHeight();
    this.cachedHeightNoScrollbar = this.CalculateTotalRequiredHeight();
    this.RecacheSize();
    this.RecacheColumnWidths();
  }

  private void RecachePawns()
  {
    this.cachedPawns.Clear();
    this.cachedPawns.AddRange(this.pawnsGetter());
    this.cachedPawns = this.LabelSortFunction((IEnumerable<Pawn>) this.cachedPawns).ToList<Pawn>();
    if (this.sortByColumn != null)
    {
      if (this.sortDescending)
        this.cachedPawns.SortStable<Pawn>(new Func<Pawn, Pawn, int>(this.sortByColumn.Worker.Compare));
      else
        this.cachedPawns.SortStable<Pawn>((Func<Pawn, Pawn, int>) ((a, b) => this.sortByColumn.Worker.Compare(b, a)));
    }
    this.cachedPawns = this.PrimarySortFunction((IEnumerable<Pawn>) this.cachedPawns).ToList<Pawn>();
  }

  protected new virtual IEnumerable<Pawn> LabelSortFunction(IEnumerable<Pawn> input) => (IEnumerable<Pawn>) input.OrderBy<Pawn, string>((Func<Pawn, string>) (p => p.Label));

  protected new virtual IEnumerable<Pawn> PrimarySortFunction(IEnumerable<Pawn> input) => input;

  private void RecacheColumnWidths()
  {
    float totalAvailableSpaceForColumns = this.cachedSize.x - 16f;
    float minWidthsSum = 0.0f;
    this.RecacheColumnWidths_StartWithMinWidths(out minWidthsSum);
    if ((double) minWidthsSum == (double) totalAvailableSpaceForColumns)
      return;
    if ((double) minWidthsSum > (double) totalAvailableSpaceForColumns)
    {
      this.SubtractProportionally(minWidthsSum - totalAvailableSpaceForColumns, minWidthsSum);
    }
    else
    {
      bool noMoreFreeSpace;
      this.RecacheColumnWidths_DistributeUntilOptimal(totalAvailableSpaceForColumns, ref minWidthsSum, out noMoreFreeSpace);
      if (noMoreFreeSpace)
        return;
      this.RecacheColumnWidths_DistributeAboveOptimal(totalAvailableSpaceForColumns, ref minWidthsSum);
    }
  }

  private void RecacheColumnWidths_StartWithMinWidths(out float minWidthsSum)
  {
    minWidthsSum = 0.0f;
    this.cachedColumnWidths.Clear();
    List<PawnColumnDef> columns = this.Columns;
    for (int index = 0; index < columns.Count; ++index)
    {
      float minWidth = this.GetMinWidth(columns[index]);
      this.cachedColumnWidths.Add(minWidth);
      minWidthsSum += minWidth;
    }
  }

  private void RecacheColumnWidths_DistributeUntilOptimal(
    float totalAvailableSpaceForColumns,
    ref float usedWidth,
    out bool noMoreFreeSpace)
  {
    this.columnAtOptimalWidth.Clear();
    List<PawnColumnDef> columns = this.Columns;
    for (int index = 0; index < columns.Count; ++index)
      this.columnAtOptimalWidth.Add((double) this.cachedColumnWidths[index] >= (double) this.GetOptimalWidth(columns[index]));
    int num1 = 0;
    bool flag1;
    bool flag2;
    do
    {
      ++num1;
      if (num1 >= 10000)
      {
        Log.Error("Too many iterations.");
        break;
      }
      float a = float.MinValue;
      for (int index = 0; index < columns.Count; ++index)
      {
        if (!this.columnAtOptimalWidth[index])
          a = Mathf.Max(a, (float) columns[index].widthPriority);
      }
      float num2 = 0.0f;
      for (int index = 0; index < this.cachedColumnWidths.Count; ++index)
      {
        if (!this.columnAtOptimalWidth[index] && (double) columns[index].widthPriority == (double) a)
          num2 += this.GetOptimalWidth(columns[index]);
      }
      float num3 = totalAvailableSpaceForColumns - usedWidth;
      flag1 = false;
      flag2 = false;
      for (int index = 0; index < this.cachedColumnWidths.Count; ++index)
      {
        if (!this.columnAtOptimalWidth[index])
        {
          if ((double) columns[index].widthPriority != (double) a)
          {
            flag1 = true;
          }
          else
          {
            float num4 = num3 * this.GetOptimalWidth(columns[index]) / num2;
            float num5 = this.GetOptimalWidth(columns[index]) - this.cachedColumnWidths[index];
            if ((double) num4 >= (double) num5)
            {
              num4 = num5;
              this.columnAtOptimalWidth[index] = true;
              flag2 = true;
            }
            else
              flag1 = true;
            if ((double) num4 > 0.0)
            {
              this.cachedColumnWidths[index] += num4;
              usedWidth += num4;
            }
          }
        }
      }
      if ((double) usedWidth >= (double) totalAvailableSpaceForColumns - 0.10000000149011612)
      {
        noMoreFreeSpace = true;
        break;
      }
    }
    while (flag1 && flag2);
    noMoreFreeSpace = false;
  }

  private void RecacheColumnWidths_DistributeAboveOptimal(
    float totalAvailableSpaceForColumns,
    ref float usedWidth)
  {
    this.columnAtMaxWidth.Clear();
    List<PawnColumnDef> columns = this.Columns;
    for (int index = 0; index < columns.Count; ++index)
      this.columnAtMaxWidth.Add((double) this.cachedColumnWidths[index] >= (double) this.GetMaxWidth(columns[index]));
    int num1 = 0;
    bool flag;
    do
    {
      ++num1;
      if (num1 >= 10000)
      {
        Log.Error("Too many iterations.");
        return;
      }
      float num2 = 0.0f;
      for (int index = 0; index < columns.Count; ++index)
      {
        if (!this.columnAtMaxWidth[index])
          num2 += Mathf.Max(this.GetOptimalWidth(columns[index]), 1f);
      }
      float num3 = totalAvailableSpaceForColumns - usedWidth;
      flag = false;
      for (int index = 0; index < columns.Count; ++index)
      {
        if (!this.columnAtMaxWidth[index])
        {
          float num4 = num3 * Mathf.Max(this.GetOptimalWidth(columns[index]), 1f) / num2;
          float num5 = this.GetMaxWidth(columns[index]) - this.cachedColumnWidths[index];
          if ((double) num4 >= (double) num5)
          {
            num4 = num5;
            this.columnAtMaxWidth[index] = true;
          }
          else
            flag = true;
          if ((double) num4 > 0.0)
          {
            this.cachedColumnWidths[index] += num4;
            usedWidth += num4;
          }
        }
      }
      if ((double) usedWidth >= (double) totalAvailableSpaceForColumns - 0.10000000149011612)
        goto label_23;
    }
    while (flag);
    goto label_22;
    label_23:
    return;
    label_22:
    this.DistributeRemainingWidthProportionallyAboveMax(totalAvailableSpaceForColumns - usedWidth);
  }

  private void RecacheRowHeights()
  {
    this.cachedRowHeights.Clear();
    for (int index = 0; index < this.cachedPawns.Count; ++index)
      this.cachedRowHeights.Add(this.CalculateRowHeight(this.cachedPawns[index]));
  }

  private void RecacheSize()
  {
    if (this.hasFixedSize)
    {
      this.cachedSize = this.fixedSize;
    }
    else
    {
      float num = 0.0f;
      List<PawnColumnDef> columns = this.Columns;
      for (int index = 0; index < columns.Count; ++index)
      {
        if (!columns[index].ignoreWhenCalculatingOptimalTableSize)
          num += this.GetOptimalWidth(columns[index]);
      }
      float a1 = Mathf.Clamp(num + 16f, (float) this.minTableWidth, (float) this.maxTableWidth);
      float a2 = Mathf.Clamp(this.cachedHeightNoScrollbar, (float) this.minTableHeight, (float) this.maxTableHeight);
      this.cachedSize = new Vector2(Mathf.Min(a1, (float) UI.screenWidth), Mathf.Min(a2, (float) UI.screenHeight));
    }
  }

  private void SubtractProportionally(float toSubtract, float totalUsedWidth)
  {
    for (int index = 0; index < this.cachedColumnWidths.Count; ++index)
      this.cachedColumnWidths[index] -= toSubtract * this.cachedColumnWidths[index] / totalUsedWidth;
  }

  private void DistributeRemainingWidthProportionallyAboveMax(float toDistribute)
  {
    float num = 0.0f;
    List<PawnColumnDef> columns = this.Columns;
    for (int index = 0; index < columns.Count; ++index)
      num += Mathf.Max(this.GetOptimalWidth(columns[index]), 1f);
    for (int index = 0; index < columns.Count; ++index)
      this.cachedColumnWidths[index] += toDistribute * Mathf.Max(this.GetOptimalWidth(columns[index]), 1f) / num;
  }

  private float GetOptimalWidth(PawnColumnDef column) => Mathf.Max((float) column.Worker.GetOptimalWidth(this), 0.0f);

  private float GetMinWidth(PawnColumnDef column) => Mathf.Max((float) column.Worker.GetMinWidth(this), 0.0f);

  private float GetMaxWidth(PawnColumnDef column) => Mathf.Max((float) column.Worker.GetMaxWidth(this), 0.0f);

  private float CalculateRowHeight(Pawn pawn)
  {
    float a = 0.0f;
    List<PawnColumnDef> columns = this.Columns;
    for (int index = 0; index < columns.Count; ++index)
      a = Mathf.Max(a, (float) columns[index].Worker.GetMinCellHeight(pawn));
    return a;
  }

  private float CalculateHeaderHeight()
  {
    float a = 0.0f;
    List<PawnColumnDef> columns = this.Columns;
    for (int index = 0; index < columns.Count; ++index)
      a = Mathf.Max(a, (float) columns[index].Worker.GetMinHeaderHeight(this));
    return a;
  }

  private float CalculateTotalRequiredHeight()
  {
    float headerHeight = this.CalculateHeaderHeight();
    for (int index = 0; index < this.cachedPawns.Count; ++index)
      headerHeight += this.CalculateRowHeight(this.cachedPawns[index]);
    return headerHeight;
  }
}