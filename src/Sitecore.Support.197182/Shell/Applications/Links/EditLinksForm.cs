namespace Sitecore.Support.Shell.Applications.Links
{
  using System;
  using System.IO;
  using System.Web.UI;

  using Sitecore.Configuration;
  using Sitecore.Data;
  using Sitecore.Data.Fields;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;
  using Sitecore.Globalization;
  using Sitecore.Links;
  using Sitecore.Resources;
  using Sitecore.SecurityModel;
  using Sitecore.Shell.Applications.Dialogs.ItemLister;
  using Sitecore.Text;
  using Sitecore.Web;
  using Sitecore.Web.UI.HtmlControls;
  using Sitecore.Web.UI.Pages;
  using Sitecore.Web.UI.Sheer;

  using Version = Sitecore.Data.Version;

  /// <summary>
  /// Represents a BreakingLinksForm.
  /// </summary>
  public class EditLinksForm : DialogForm
  {
    #region Fields

    /// <summary>
    /// The links.
    /// </summary>
    protected Scrollbox Links;

    #endregion

    #region Protected methods

    /// <summary>
    /// Edits the specified database name.
    /// </summary>
    /// <param name="id">
    /// The item id.
    /// </param>
    protected void Edit([NotNull] string id)
    {
      Assert.ArgumentNotNullOrEmpty(id, "id");

      var url = new UrlString("/sitecore/shell/Applications/Content Manager/default.aspx");

      url.Append("fo", id);
      url.Append("mo", "popup");

      Context.ClientPage.ClientResponse.ShowModalDialog(url.ToString(), "900", "560");
    }

    /// <summary>
    /// Handles a click on the Cancel button.
    /// </summary>
    /// <param name="sender">
    /// The event owner object.
    /// </param>
    /// <param name="args">
    /// The event arguments.
    /// </param>
    /// <remarks>
    /// When the user clicksCancel, the dialog is closed by calling
    /// the <see cref="Sitecore.Web.UI.Sheer.ClientResponse.CloseWindow">CloseWindow</see> method.
    /// </remarks>
    protected override void OnCancel([NotNull] object sender, [NotNull] EventArgs args)
    {
      Assert.ArgumentNotNull(sender, "sender");
      Assert.ArgumentNotNull(args, "args");

      Context.ClientPage.ClientResponse.SetDialogValue("no");
      base.OnCancel(sender, args);
    }

    /// <summary>
    /// Raises the load event.
    /// </summary>
    /// <param name="e">
    /// The <see cref="System.EventArgs"/> instance containing the event data.
    /// </param>
    /// <remarks>
    /// This method notifies the server control that it should perform actions common to each HTTP
    /// request for the page it is associated with, such as setting up a database query. At this
    /// stage in the page lifecycle, server controls in the hierarchy are created and initialized,
    /// view state is restored, and form controls reflect client-side data. Use the IsPostBack
    /// property to determine whether the page is being loaded in response to a client postback,
    /// or if it is being loaded and accessed for the first time.
    /// </remarks>
    protected override void OnLoad([NotNull] EventArgs e)
    {
      Assert.ArgumentNotNull(e, "e");

      base.OnLoad(e);

      if (!Context.ClientPage.IsEvent)
      {
        this.BuildReport();
      }
    }

    /// <summary>
    /// Handles a click on the OK button.
    /// </summary>
    /// <param name="sender">
    /// The event owner.
    /// </param>
    /// <param name="args">
    /// The event arguments.
    /// </param>
    /// <remarks>
    /// When the user clicks OK, the dialog is closed by calling
    /// the <see cref="Sitecore.Web.UI.Sheer.ClientResponse.CloseWindow">CloseWindow</see> method.
    /// </remarks>
    protected override void OnOK([NotNull] object sender, [NotNull] EventArgs args)
    {
      Assert.ArgumentNotNull(sender, "sender");
      Assert.ArgumentNotNull(args, "args");

      Context.ClientPage.ClientResponse.SetDialogValue("yes");
      base.OnOK(sender, args);
    }

    /// <summary>
    /// Removes the specified database name.
    /// </summary>
    /// <param name="targetDatabaseName">
    /// Name of the database.
    /// </param>
    /// <param name="targetItemID">
    /// The target item id.
    /// </param>
    /// <param name="targetPath">
    /// The target path.
    /// </param>
    /// <param name="sourceDatabaseName">
    /// Name of the source database.
    /// </param>
    /// <param name="sourceItemID">
    /// The source item ID.
    /// </param>
    /// <param name="sourceFieldID">
    /// The source field ID.
    /// </param>
    /// <param name="linkID">
    /// The link hashcode.
    /// </param>
    protected void Relink(
      [NotNull] string targetDatabaseName,
      [NotNull] string targetItemID,
      string targetPath,
      [NotNull] string sourceDatabaseName,
      [NotNull] string sourceItemID,
      [NotNull] string sourceFieldID,
      [NotNull] string linkID)
    {
      Assert.ArgumentNotNullOrEmpty(targetDatabaseName, "targetDatabaseName");
      Assert.ArgumentNotNullOrEmpty(targetItemID, "targetItemID");
      Assert.ArgumentNotNullOrEmpty(sourceDatabaseName, "sourceDatabaseName");
      Assert.ArgumentNotNullOrEmpty(sourceItemID, "sourceItemID");
      Assert.ArgumentNotNullOrEmpty(sourceFieldID, "sourceFieldID");
      Assert.ArgumentNotNullOrEmpty(linkID, "linkID");

      var args = Context.ClientPage.CurrentPipelineArgs as ClientPipelineArgs;
      Assert.IsNotNull(args, typeof(ClientPipelineArgs));

      Database database = Factory.GetDatabase(targetDatabaseName);
      Assert.IsNotNull(database, typeof(Database), "Database: {0}", targetDatabaseName);

      Item targetItem = database.GetItem(targetItemID);
      Assert.IsNotNull(targetItem, typeof(Item), "ID: {0}", targetItemID);

      Database sourceDatabase = Factory.GetDatabase(sourceDatabaseName);
      Item sourceItem = sourceDatabase.Items[sourceItemID];
      if (sourceItem == null)
      {
        return;
      }

      if (args.IsPostBack)
      {
        if (args.HasResult)
        {
          Assert.IsNotNull(Context.ContentDatabase, "content database");
          Item item = Context.ContentDatabase.GetItem(args.Result);
          Assert.IsNotNull(item, typeof(Item), "Item \"{0}\" not found", args.Result);

          Item[] versions = sourceItem.Versions.GetVersions(true);

          foreach (Item version in versions)
          {
            Field sourceField = version.Fields[sourceFieldID];
            if (sourceField == null)
            {
              continue;
            }

            CustomField customField = FieldTypeManager.GetField(sourceField);
            if (customField == null)
            {
              continue;
            }

            using (new SecurityDisabler())
            {
              version.Editing.BeginEdit();

              var itemLink = new ItemLink(
                sourceDatabaseName,
                ID.Parse(sourceItemID),
                version.Language,
                version.Version,
                ID.Parse(sourceFieldID),
                targetDatabaseName,
                ID.Parse(targetItemID),
                Language.Invariant,
                Version.Latest,
                targetPath);

              customField.Relink(itemLink, item);

              version.Editing.EndEdit();
            }
          }

          SheerResponse.Remove(linkID);

          SheerResponse.Alert(Texts.THE_LINK_HAS_BEEN_CHANGED);
        }
      }
      else
      {
        var options = new SelectItemOptions();

        options.Icon = "Network/16x16/link_new.png";
        options.Title = Texts.RELINK;
        options.Text = Texts.SELECT_THE_ITEM_THAT_YOU_WANT_THE_LINK_TO_POINT_TO_THEN_CLICK_RELINK_TO_SET_THE_LINK;
        options.ButtonText = "Relink";
        options.SelectedItem = targetItem;

        SheerResponse.ShowModalDialog(options.ToUrlString().ToString(), true);

        args.WaitForPostBack();
      }
    }

    /// <summary>
    /// Removes the specified database name.
    /// </summary>
    /// <param name="targetDatabaseName">
    /// Name of the database.
    /// </param>
    /// <param name="targetItemID">
    /// The target item id.
    /// </param>
    /// <param name="targetPath">
    /// The target path.
    /// </param>
    /// <param name="sourceDatabaseName">
    /// Name of the source database.
    /// </param>
    /// <param name="sourceItemID">
    /// The source item ID.
    /// </param>
    /// <param name="sourceFieldID">
    /// The source field ID.
    /// </param>
    /// <param name="linkID">
    /// The link hashcode.
    /// </param>
    protected void Remove(
      [NotNull] string targetDatabaseName,
      [NotNull] string targetItemID,
      string targetPath,
      [NotNull] string sourceDatabaseName,
      [NotNull] string sourceItemID,
      [NotNull] string sourceFieldID,
      [NotNull] string linkID)
    {
      Assert.ArgumentNotNullOrEmpty(targetDatabaseName, "targetDatabaseName");
      Assert.ArgumentNotNullOrEmpty(targetItemID, "targetItemID");
      Assert.ArgumentNotNullOrEmpty(sourceDatabaseName, "sourceDatabaseName");
      Assert.ArgumentNotNullOrEmpty(sourceItemID, "sourceItemID");
      Assert.ArgumentNotNullOrEmpty(sourceFieldID, "sourceFieldID");
      Assert.ArgumentNotNullOrEmpty(linkID, "linkID");

      Database database = Factory.GetDatabase(targetDatabaseName);
      Assert.IsNotNull(database, typeof(Database), "Database: {0}", targetDatabaseName);

      Item targetItem = database.GetItem(targetItemID);
      Assert.IsNotNull(targetItem, typeof(Item), "ID: {0}", targetItemID);

      Database sourceDatabase = Factory.GetDatabase(sourceDatabaseName);
      Item sourceItem = sourceDatabase.Items[sourceItemID];
      if (sourceItem == null)
      {
        return;
      }

      Item[] versions = sourceItem.Versions.GetVersions(true);

      foreach (Item version in versions)
      {
        Field sourceField = version.Fields[sourceFieldID];
        if (sourceField == null)
        {
          continue;
        }

        CustomField customField = FieldTypeManager.GetField(sourceField);
        if (customField == null)
        {
          continue;
        }

        using (new SecurityDisabler())
        {
          version.Editing.BeginEdit();

          var itemLink = new ItemLink(
            sourceDatabaseName,
            ID.Parse(sourceItemID),
            version.Language,
            version.Version,
            ID.Parse(sourceFieldID),
            targetDatabaseName,
            ID.Parse(targetItemID),
            Language.Invariant,
            Version.Latest,
            targetPath);

          customField.RemoveLink(itemLink);

          version.Editing.EndEdit();
        }
      }

      SheerResponse.Remove(linkID);

      SheerResponse.Alert(Texts.THE_LINK_HAS_BEEN_REMOVED);
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Builds the report.
    /// </summary>
    /// <param name="output">
    /// The output.
    /// </param>
    /// <param name="linkDatabase">
    /// The link database.
    /// </param>
    /// <param name="item">
    /// The Sitecore item.
    /// </param>
    private static void BuildReport(
      [NotNull] HtmlTextWriter output, [NotNull] LinkDatabase linkDatabase, [NotNull] Item item)
    {
      Assert.ArgumentNotNull(output, "output");
      Assert.ArgumentNotNull(linkDatabase, "linkDatabase");
      Assert.ArgumentNotNull(item, "item");

      bool ignoreClones = WebUtil.GetQueryString("ignoreclones") == "1";
      ItemLink[] links = linkDatabase.GetReferrers(item);
      if (links.Length > 0)
      {
        foreach (ItemLink link in links)
        {
          if (ignoreClones && (link.SourceFieldID == FieldIDs.SourceItem || link.SourceFieldID == FieldIDs.Source))
          {
            continue;
          }

          Database database = Factory.GetDatabase(link.SourceDatabaseName, false);
          if (database == null)
          {
            continue;
          }

          Item referer = database.Items[link.SourceItemID];
          if (referer == null)
          {
            continue;
          }

          string id = "L" + ID.NewID.ToShortID();

          output.Write("<div id=\"" + id + "\" class=\"scLink\">");

          output.Write("<table class=\"scLinkTable\" cellpadding=\"0\" cellspacing=\"0\"><tr>");

          output.Write("<td>");

          var icon = new ImageBuilder();
          icon.Src = ImageBuilder.ResizeImageSrc(referer.Appearance.Icon, 24, 24);
          icon.Class = "scLinkIcon";
          output.Write(icon.ToString());

          output.Write("</td>");

          output.Write("<td>");

          output.Write("<div class=\"scLinkHeader\">");
          output.Write(referer.DisplayName);
          output.Write("</div>");

          output.Write("<div class=\"scLinkDetails\">");
          output.Write(referer.Paths.ContentPath);
          output.Write("</div>");

          WriteDivider(output);

          output.Write("<div class=\"scLinkField\">");
          output.Write(Translate.Text(Texts.FIELD));
          output.Write(' ');
          if (link.SourceFieldID.IsNull)
          {
            output.Write(Translate.Text(Texts.TEMPLATE1));
          }
          else
          {
            Field field = referer.Fields[link.SourceFieldID];

            if (field.GetTemplateField() != null)
            {
              output.Write(field.DisplayName);
            }
            else
            {
              output.Write(Translate.Text(Texts.UNKNOWN_FIELD));
            }
          }

          output.Write("</div>");

          output.Write("<div class=\"scLinkField\">");
          output.Write(Translate.Text(Texts.TARGET));
          output.Write(' ');
          output.Write(item.Paths.ContentPath);
          output.Write("</div>");

          WriteDivider(output);

          string parameters = "(\"" + link.TargetDatabaseName + "\",\"" + link.TargetItemID + "\",\"" + link.TargetPath +
                              "\",\"" + link.SourceDatabaseName + "\",\"" + link.SourceItemID + "\",\"" +
                              link.SourceFieldID + "\",\"" + id + "\")";

          string removeClick = "Remove" + parameters;
          string relinkClick = "Relink" + parameters;

          WriteCommand(output, "Edit", "Applications/16x16/edit.png", "Edit(\"" + referer.ID + "\")");
          WriteCommand(output, "Remove Link", "Network/16x16/link_delete.png", removeClick);
          WriteCommand(output, "Link to Other Item", "Network/16x16/link_new.png", relinkClick);

          output.Write("</td>");

          output.Write("</tr></table>");

          output.Write("</div>");
        }
      }

      foreach (Item child in item.Children)
      {
        BuildReport(output, linkDatabase, child);
      }
    }

    /// <summary>
    /// Writes the command.
    /// </summary>
    /// <param name="output">
    /// The output.
    /// </param>
    /// <param name="header">
    /// The header.
    /// </param>
    /// <param name="icon">
    /// The icon path.
    /// </param>
    /// <param name="click">
    /// The click action.
    /// </param>
    private static void WriteCommand(
      [NotNull] HtmlTextWriter output, [NotNull] string header, [NotNull] string icon, [NotNull] string click)
    {
      Assert.ArgumentNotNull(output, "output");
      Assert.ArgumentNotNullOrEmpty(header, "header");
      Assert.ArgumentNotNullOrEmpty(icon, "icon");
      Assert.ArgumentNotNullOrEmpty(click, "click");

      output.Write(
        "<span class=\"scLinkCommand scRollOver\" onmouseover=\"javascript:return scForm.rollOver(this, event)\" onfocus=\"javascript:return scForm.rollOver(this, event)\" onmouseout=\"javascript:return scForm.rollOver(this, event)\" onblur=\"javascript:return scForm.rollOver(this, event)\" onclick=\"" +
        Context.ClientPage.GetClientEvent(click) + "\">");

      var image = new ImageBuilder();
      image.Src = icon;
      image.Class = "scLinkCommandIcon";
      output.Write(image.ToString());

      output.Write(Translate.Text(header));

      output.Write("</span>");
    }

    /// <summary>
    /// Writes the divider.
    /// </summary>
    /// <param name="output">
    /// The output.
    /// </param>
    private static void WriteDivider([NotNull] HtmlTextWriter output)
    {
      Assert.ArgumentNotNull(output, "output");

      output.Write("<div class=\"scLinkDivider\">");
      output.Write(Images.GetSpacer(1, 1));
      output.Write("</div>");
    }

    /// <summary>
    /// Builds the report.
    /// </summary>
    private void BuildReport()
    {
      var output = new HtmlTextWriter(new StringWriter());

      var list = new ListString(UrlHandle.Get()["list"]);
      UrlHandle.DisposeHandle(UrlHandle.Get());

      LinkDatabase linkDatabase = Globals.LinkDatabase;

      foreach (string id in list)
      {
        Assert.IsNotNull(Context.ContentDatabase, "content database");
        Item item = Context.ContentDatabase.Items[id];
        Assert.IsNotNull(item, "item");
        BuildReport(output, linkDatabase, item);
      }

      this.Links.InnerHtml = output.InnerWriter.ToString();
    }

    #endregion
  }
}