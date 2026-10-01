# Remover recursos depois da entrega

Os recursos deste checkpoint usam o grupo `561439-dimdim-rg`. O grupo mantém seus
metadados em `eastus2`; o servidor SQL e o banco foram criados em `mexicocentral`
após recusas de provisionamento nas regiões inicialmente testadas.

Após concluir a entrega e salvar as evidências, confira o conteúdo do grupo:

```powershell
az resource list --resource-group 561439-dimdim-rg -o table
```

Para excluir o grupo e todos os seus recursos, incluindo o banco e os dados:

```powershell
az group delete --name 561439-dimdim-rg
```

Confirme a exclusão quando o Azure CLI perguntar. A exclusão é permanente; salve antes as evidências e quaisquer dados necessários.

Confira a conclusão:

```powershell
az group exists --name 561439-dimdim-rg
```

O resultado deve ser `false`. Parar apenas a API não remove o plano, o banco ou o monitoramento.
