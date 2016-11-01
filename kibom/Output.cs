﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.RtfRendering;
using PdfSharp.Pdf;
using System.IO;

namespace kibom
{
	class Output
	{
		public static void OutputTSV(List<DesignatorGroup> groups, string file)
		{
			using (StreamWriter sw = new StreamWriter(file))
			{
				foreach (DesignatorGroup g in groups)
				{
					sw.WriteLine("Group: " + g.designator + " (" + g.comp_list.Count.ToString() + ")");
					DefaultComp def = Component.FindDefaultComp(g.designator);
					if (def != null)
					{
						sw.Write("(" + def.long_name);
						if (def.has_default)
							sw.Write(", " + def.default_type + " unless otherwise stated");
						sw.WriteLine(")");
					}
					foreach (Component c in g.comp_list)
						sw.WriteLine("\t" + c.reference +
											"\t" + c.value +
											"\t" + c.footprint_normalized +
											"\t" + c.precision);
					sw.WriteLine();
				}
			}
		}

		public static void OutputPDF(List<DesignatorGroup> groups, string file)
		{
			// document setup
			var doc = new Document();
			doc.DefaultPageSetup.PageFormat = PageFormat.A4;
			doc.DefaultPageSetup.Orientation = Orientation.Landscape;
			doc.DefaultPageSetup.TopMargin = "1.5cm";
			doc.DefaultPageSetup.BottomMargin = "1.5cm";
			doc.DefaultPageSetup.LeftMargin = "1.5cm";
			doc.DefaultPageSetup.RightMargin = "1.5cm";
			doc.Styles["Normal"].Font.Name = "Arial";

			var footer = new Paragraph();
			footer.AddTab();
			footer.AddPageField();
			footer.AddText(" of ");
			footer.AddNumPagesField();
			footer.Format.Alignment = ParagraphAlignment.Center;

			// generate content
			var section = doc.AddSection();
			section.Footers.Primary.Add(footer.Clone());
			section.Footers.EvenPage.Add(footer.Clone());
			var para = section.AddParagraph();
			var table = PDFCreateTable(ref section);

			int i = 1;
			foreach (DesignatorGroup g in groups)
			{
				// check for groups that are entire "no part"
				bool all_no_part = true;
				foreach (Component c in g.comp_list)
				{
					if (c.footprint_normalized != "no part")
						all_no_part = false;
				}
				if (all_no_part)
					continue;

				// group header row
				var row = table.AddRow();
				row.Shading.Color = Colors.LightGray;
				row.Cells[0].MergeRight = 6;
				row.Cells[0].Format.Alignment = ParagraphAlignment.Left;

				DefaultComp def = Component.FindDefaultComp(g.designator);
				if (def != null)
				{
					var p = row.Cells[0].AddParagraph(def.long_name);
					p.Format.Font.Bold = true;
					if (def.has_default)
						row.Cells[0].AddParagraph("All " + def.default_type + " unless otherwise stated");
				}
				else
				{
					var p = row.Cells[0].AddParagraph(g.designator);
					p.Format.Font.Bold = true;
				}

				foreach (Component c in g.comp_list)
				{
					if (c.footprint_normalized == "no part")
						continue;

					row = table.AddRow();
					row.Cells[0].AddParagraph(i.ToString());
					row.Cells[1].AddParagraph(c.count.ToString());
					row.Cells[2].AddParagraph(c.reference);
					row.Cells[3].AddParagraph(c.value);

					string temp = c.footprint_normalized;
					if (c.code != null)
						temp += ", " + c.code;
					if (c.precision != null)
						temp += ", " + c.precision;
					row.Cells[4].AddParagraph(temp);
					//row.Cells[4].AddParagraph(c.footprint_normalized);

					row.Cells[5].AddParagraph(c.part_no);
					row.Cells[6].AddParagraph(c.note);
				}
			}

			// generate PDF file
			var pdfRenderer = new PdfDocumentRenderer(true, PdfFontEmbedding.Always);
			pdfRenderer.Document = doc;
			pdfRenderer.RenderDocument();
			pdfRenderer.PdfDocument.Save(file);
		}

		static Table PDFCreateTable(ref Section section)
		{
			var table = section.AddTable();

			table.Borders.Width = 1;
			table.TopPadding = 1;
			table.BottomPadding = 2;
			table.LeftPadding = 5;
			table.RightPadding = 5;

			var col = table.AddColumn("1.5cm");	// item
			col.Format.Alignment = ParagraphAlignment.Center;
			col = table.AddColumn("1.5cm");	// quantity
			col.Format.Alignment = ParagraphAlignment.Center;
			col = table.AddColumn("3.5cm");	// reference
			col.Format.Alignment = ParagraphAlignment.Left;
			col = table.AddColumn("4.5cm");	// value
			col.Format.Alignment = ParagraphAlignment.Left;
			col = table.AddColumn("4.5cm");	// type
			col.Format.Alignment = ParagraphAlignment.Left;
			//col = table.AddColumn("2.5cm");	// mechanical/size
			//col.Format.Alignment = ParagraphAlignment.Left;
			col = table.AddColumn("5.5cm");	// manufacturer part number
			col.Format.Alignment = ParagraphAlignment.Left;
			col = table.AddColumn("6cm");	// notes
			col.Format.Alignment = ParagraphAlignment.Left;

			var row = table.AddRow();
			row.HeadingFormat = true;
			row.Format.Alignment = ParagraphAlignment.Center;
			row.Format.Font.Bold = true;
			row.Shading.Color = Colors.LightGray;
			row.Cells[0].AddParagraph("No.");
			row.Cells[1].AddParagraph("Qty.");
			row.Cells[2].AddParagraph("Reference");
			row.Cells[3].AddParagraph("Value");
			row.Cells[4].AddParagraph("Type");
			//row.Cells[5].AddParagraph("Size");
			row.Cells[5].AddParagraph("Manufacturer Part No.");
			row.Cells[6].AddParagraph("Notes");

			return table;
		}

	}
}