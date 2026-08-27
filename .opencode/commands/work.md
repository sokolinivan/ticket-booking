---
description: Run tests with coverage
agent: build
---

/comet Возьми задачу $ARGUMENTS из файла @docs/plan.md.
Веди диалог на русском
Выходные артефакты по возможности делай на русском.
Если что-то изменилось в AppHost или у сервиса появилась settings-переменная, обнови корнейвой docker-compose файл.
Только в случае успешного завершения задачи отметь $ARGUMENTS в файле @docs/plan.md как выполненный.
