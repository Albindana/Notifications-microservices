import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api, errorMessage } from '../../lib/api'
import { Spinner } from '../../components/Spinner'
import { useToast } from '../../components/Toast'
import type { NotificationTemplate } from '../../types'

async function getTemplates(): Promise<NotificationTemplate[]> {
  const { data } = await api.get<NotificationTemplate[]>('/templates')
  return data
}

async function updateTemplate(input: {
  id: string
  subject: string
  bodyTemplate: string
}): Promise<NotificationTemplate> {
  const { data } = await api.put<NotificationTemplate>(`/templates/${input.id}`, {
    subject: input.subject,
    bodyTemplate: input.bodyTemplate,
  })
  return data
}

export function TemplatesPage() {
  const { push } = useToast()
  const queryClient = useQueryClient()
  const query = useQuery({ queryKey: ['templates'], queryFn: getTemplates })

  return (
    <div>
      <h1 className="mb-1 text-2xl font-bold text-slate-800">Templates</h1>
      <p className="mb-6 text-sm text-slate-500">
        Placeholders like <code>{'{{FirstName}}'}</code> and{' '}
        <code>{'{{ResetLink}}'}</code> are filled in when a notification is sent.
      </p>

      {query.isLoading ? (
        <Spinner label="Loading templates…" />
      ) : query.isError ? (
        <div className="card p-6 text-sm text-rose-600">
          Couldn't load templates.
        </div>
      ) : (
        <div className="space-y-4">
          {query.data?.map((t) => (
            <TemplateCard
              key={t.id}
              template={t}
              onSaved={() => {
                push('Template saved', 'success')
                queryClient.invalidateQueries({ queryKey: ['templates'] })
              }}
            />
          ))}
        </div>
      )}
    </div>
  )
}

function TemplateCard({
  template,
  onSaved,
}: {
  template: NotificationTemplate
  onSaved: () => void
}) {
  const { push } = useToast()
  const [subject, setSubject] = useState(template.subject)
  const [bodyTemplate, setBodyTemplate] = useState(template.bodyTemplate)

  const dirty =
    subject !== template.subject || bodyTemplate !== template.bodyTemplate

  const mutation = useMutation({
    mutationFn: updateTemplate,
    onSuccess: onSaved,
    onError: (err) => push(errorMessage(err, 'Save failed'), 'error'),
  })

  return (
    <div className="card p-6">
      <div className="mb-4 flex items-center justify-between">
        <span className="rounded-full bg-brand-50 px-3 py-1 text-sm font-semibold text-brand-700">
          {template.type}
        </span>
        <span className="text-xs text-slate-400">
          Updated {new Date(template.updatedAt).toLocaleDateString()}
        </span>
      </div>
      <div className="space-y-3">
        <div>
          <label className="label">Subject</label>
          <input
            className="input"
            value={subject}
            onChange={(e) => setSubject(e.target.value)}
          />
        </div>
        <div>
          <label className="label">Body template</label>
          <textarea
            className="input min-h-28 font-mono text-xs"
            value={bodyTemplate}
            onChange={(e) => setBodyTemplate(e.target.value)}
          />
        </div>
      </div>
      <div className="mt-4 flex justify-end gap-2">
        {dirty && (
          <button
            className="btn-secondary"
            onClick={() => {
              setSubject(template.subject)
              setBodyTemplate(template.bodyTemplate)
            }}
          >
            Reset
          </button>
        )}
        <button
          className="btn-primary"
          disabled={!dirty || mutation.isPending}
          onClick={() =>
            mutation.mutate({ id: template.id, subject, bodyTemplate })
          }
        >
          {mutation.isPending ? 'Saving…' : 'Save'}
        </button>
      </div>
    </div>
  )
}
