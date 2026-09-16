contraband-examine-text-Minor =
    { $type ->
       *[item] [color={ $color }]This item is considered minor contraband.[/color]
        [reagent] [color={ $color }]This reagent is considered minor contraband.[/color]
    }
contraband-examine-text-Restricted =
    { $type ->
       *[item] [color={ $color }]This item is departmentally restricted.[/color]
        [reagent] [color={ $color }]This reagent is departmentally restricted.[/color]
    }
contraband-examine-text-FirstLevel = [color=yellow]Это предмет [bold]1[/bold] класса опасности.[/color]
contraband-examine-text-Major =
    { $type ->
       *[item] [color={ $color }]This item is considered major contraband.[/color]
        [reagent] [color={ $color }]This reagent is considered major contraband.[/color]
    }
contraband-examine-text-SecondLevel = [color=orange]Это предмет [bold]2[/bold] класса опасности.[/color]
contraband-examine-text-Highly-Illegal =
    { $type ->
       *[item] [color={ $color }]This item is highly illegal contraband![/color]
        [reagent] [color={ $color }]This reagent is highly illegal contraband![/color]
    }
contraband-examine-text-Syndicate =
    { $type ->
       *[item] [color={ $color }]This item is highly illegal Syndicate contraband![/color]
        [reagent] [color={ $color }]This reagent is highly illegal Syndicate contraband![/color]
    }
contraband-examine-text-Magical =
    { $type ->
       *[item] [color={ $color }]This item is highly illegal magical contraband![/color]
        [reagent] [color={ $color }]This reagent is highly illegal magical contraband![/color]
    }
contraband-examine-text-ThirdLevel = [color=red]Это предмет [bold]3[/bold] класса опасности.[/color]
contraband-examine-text-Restricted-department =
    { $type ->
       *[item] [color=yellow]Этот предмет может носить: { $departments }[/color]
        [reagent] [color=yellow]Оборот этого вещества разрешен только: { $departments }[/color]
    }
contraband-examine-text-GrandTheft = [color=red]Это особо-ценный предмет![/color]
contraband-examine-text-avoid-carrying-around = [color=red][italic]Вы [bold]не[/bold] можете носить этот предмет.[/italic][/color]
contraband-examine-text-in-the-clear = [color=green][italic]Вы можете носить этот предмет.[/italic][/color]
contraband-examinable-verb-text = Легальность
contraband-examinable-verb-message = Проверить легальность этого предмета.
contraband-department-plural = { $department }
contraband-job-plural = { $job }
