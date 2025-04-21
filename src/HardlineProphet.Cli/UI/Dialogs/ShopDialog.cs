// src/HardlineProphet/UI/Dialogs/ShopDialog.cs
using HardlineProphet.Core.Models; // Item, GameState
using Terminal.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using NStack; // For ustring

namespace HardlineProphet.UI.Dialogs;

public class ShopDialog : Dialog
{
    // Declare controls as fields for class-wide access
    private ListView _itemListView;
    private TextView _itemDescriptionView;
    private Label _creditsLabel;
    private Button _purchaseButton;
    private Button _closeButton; // Changed from local var to field
    private List<Item> _shopItems;

    public bool PurchaseMade { get; private set; } = false;

    public ShopDialog()
    {
        Title = "Cyberdeck Shop";
        ColorScheme = Colors.Dialog;

        var currentCredits = ApplicationState.CurrentGameState?.Credits ?? 0;
        _shopItems = ApplicationState.LoadedItems?.Values.ToList() ?? new List<Item>();

        _creditsLabel = new Label($"Credits: {currentCredits:N0}") { X = 1, Y = 0 };

        var listFrame = new FrameView("Available Upgrades")
        { X = 0, Y = Pos.Bottom(_creditsLabel), Width = Dim.Percent(40), Height = Dim.Fill(4) };
        _itemListView = new ListView()
        { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), AllowsMarking = false, AllowsMultipleSelection = false };
        _itemListView.SetSource(_shopItems.Select(item => $"{item.Name} ({item.Cost} Cr)").ToList());
        _itemListView.SelectedItemChanged += ItemSelected;
        listFrame.Add(_itemListView);

        var descFrame = new FrameView("Description / Effect")
        { X = Pos.Right(listFrame) + 1, Y = Pos.Top(listFrame), Width = Dim.Fill(1), Height = Dim.Height(listFrame) };
        _itemDescriptionView = new TextView()
        { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true, WordWrap = true, ColorScheme = Colors.Base };
        descFrame.Add(_itemDescriptionView);

        _purchaseButton = new Button("Purchase")
        { X = Pos.Center() - 15, Y = Pos.AnchorEnd(1), Enabled = false };
        _purchaseButton.Clicked += PurchaseSelectedItem;

        // Assign to the field instead of declaring a local variable
        _closeButton = new Button("Close")
        { X = Pos.Right(_purchaseButton) + 1, Y = _purchaseButton.Y, IsDefault = false };
        _closeButton.Clicked += () => { Application.RequestStop(); };

        Add(_creditsLabel, listFrame, descFrame, _purchaseButton, _closeButton);

        if (_shopItems.Any()) { ItemSelected(new ListViewItemEventArgs(0, _itemListView.Source.ToList()[0])); _itemListView.SelectedItem = 0; }
        else { _itemDescriptionView.Text = "No items available."; }
    }

    private void ItemSelected(ListViewItemEventArgs args)
    {
        int selectedIndex = args.Item;
        if (selectedIndex >= 0 && selectedIndex < _shopItems.Count)
        {
            var selectedItem = _shopItems[selectedIndex];
            _itemDescriptionView.Text = $"{selectedItem.EffectDescription}\n\nCost: {selectedItem.Cost} Credits";
            _purchaseButton.Enabled = (ApplicationState.CurrentGameState?.Credits ?? 0) >= selectedItem.Cost;
            _purchaseButton.IsDefault = _purchaseButton.Enabled;
            _closeButton.IsDefault = !_purchaseButton.Enabled; // Use field _closeButton
        }
        else
        {
            _itemDescriptionView.Text = "";
            _purchaseButton.Enabled = false;
            _closeButton.IsDefault = true; // Use field _closeButton
        }
        _itemDescriptionView.SetNeedsDisplay();
    }

    /// <summary>
    /// Handles the logic when the Purchase button is clicked.
    /// </summary>
    private void PurchaseSelectedItem()
    {
        int selectedIndex = _itemListView.SelectedItem;
        if (ApplicationState.CurrentGameState == null || ApplicationState.CurrentGameState.Stats == null || selectedIndex < 0 || selectedIndex >= _shopItems.Count)
        {
            MessageBox.ErrorQuery("Purchase Error", "Cannot complete purchase.\nNo item selected or game state error.", "OK");
            return;
        }

        var selectedItem = _shopItems[selectedIndex];
        var currentState = ApplicationState.CurrentGameState;

        if (currentState.Credits < selectedItem.Cost)
        {
            MessageBox.ErrorQuery("Insufficient Credits", $"You need {selectedItem.Cost} credits, but only have {currentState.Credits}.", "OK");
            _purchaseButton.Enabled = false;
            _purchaseButton.IsDefault = false;
            _closeButton.IsDefault = true; // Use field _closeButton
            return;
        }

        try
        {
            var newCredits = currentState.Credits - selectedItem.Cost;
            currentState.Stats.ApplyUpgrade(selectedItem);
            Console.WriteLine($"DEBUG: Stats after applying {selectedItem.Id}: HS={currentState.Stats.HackSpeed}, ST={currentState.Stats.Stealth}, DY={currentState.Stats.DataYield}");
            ApplicationState.CurrentGameState = currentState with { Credits = newCredits };
            PurchaseMade = true;
            _creditsLabel.Text = $"Credits: {newCredits:N0}";
            _creditsLabel.SetNeedsDisplay();
            ItemSelected(new ListViewItemEventArgs(selectedIndex, _itemListView.Source.ToList()[selectedIndex])); // Re-evaluate buttons
            MessageBox.Query("Purchase Successful", $"Installed {selectedItem.Name}!", "OK");
            _itemListView.SetFocus();
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Purchase Error", $"An error occurred applying the upgrade:\n{ex.Message}", "OK");
        }
    }
}
